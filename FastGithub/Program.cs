using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

namespace FastGithub
{
    public class Program
    {
        /// <summary>
        /// 程序入口
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// 创建host
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args);
            return ConfigureHostBuilder(hostBuilder);
        }

        public static IHostBuilder ConfigureHostBuilder(IHostBuilder hostBuilder)
        {
            return hostBuilder
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
                         }
                         else
                         {
                             kestrel.ListenHttpProxy();
                         }
                     });
                     webBuilder.UseSerilog((hosting, logger) =>
                     {
                         var template = "{Timestamp:O} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                         logger
                             .ReadFrom.Configuration(hosting.Configuration)
                             .Enrich.FromLogContext()
                             .WriteTo.Console(outputTemplate: template)
                             .WriteTo.File(Path.Combine("logs", @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);
                     });
                 });
        }
    }
}
