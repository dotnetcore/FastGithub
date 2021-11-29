using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Network;
using System;
using System.IO;
using System.Net;

namespace FastGithub
{
    class Program
    {
        /// <summary>
        /// 程序入口
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            ConsoleUtil.DisableQuickEdit();
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// 创建host
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .UseSystemd()
                .UseWindowsService()
                .UseDefaultServiceProvider(c =>
                {
                    c.ValidateOnBuild = false;
                })
                .ConfigureAppConfiguration(c =>
                {
                    const string APPSETTINGS = "appsettings";
                    if (Directory.Exists(APPSETTINGS) == true)
                    {
                        foreach (var file in Directory.GetFiles(APPSETTINGS, "appsettings.*.json"))
                        {
                            var jsonFile = Path.Combine(APPSETTINGS, Path.GetFileName(file));
                            c.AddJsonFile(jsonFile, true, true);
                        }
                    }
                })
                .UseSerilog((hosting, logger) =>
                {
                    var template = "{Timestamp:O} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                    logger
                        .ReadFrom.Configuration(hosting.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(outputTemplate: template)
                        .WriteTo.File(Path.Combine("logs", @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);

                    var udpLoggerPort = hosting.Configuration.GetValue(nameof(AppOptions.UdpLoggerPort), 38457);
                    logger.WriteTo.UDPSink(IPAddress.Loopback, udpLoggerPort);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseShutdownTimeout(TimeSpan.FromSeconds(1d));
                    webBuilder.UseKestrel(kestrel =>
                    {
                        kestrel.NoLimit();
                        kestrel.ListenHttpsReverseProxy();
                        kestrel.ListenHttpReverseProxy();

                        if (OperatingSystem.IsWindows())
                        {
                            kestrel.ListenSshReverseProxy();
                            kestrel.ListenGitReverseProxy();
                        }
                        else
                        {
                            kestrel.ListenHttpProxy();
                        }
                    });
                });
        }
    }
}
