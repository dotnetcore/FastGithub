using FastGithub.HttpServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// ApplicationBuilder扩展
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用http代理中间件
        /// </summary>
        /// <param name="app"></param> 
        /// <returns></returns>
        public static IApplicationBuilder UseHttpProxy(this IApplicationBuilder app)
        {
            var middleware = app.ApplicationServices.GetRequiredService<HttpProxyMiddleware>();
            return app.Use(next => context => middleware.InvokeAsync(context));
        }

        /// <summary>
        /// 使用请求日志中间件
        /// </summary>
        /// <param name="app"></param> 
        /// <returns></returns>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            var middleware = app.ApplicationServices.GetRequiredService<RequestLoggingMiddleware>();
            return app.Use(next => context => middleware.InvokeAsync(context, next));
        }

        /// <summary>
        /// 禁用请求日志中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder DisableRequestLogging(this IApplicationBuilder app)
        {
            return app.Use(next => context =>
            {
                var loggingFeature = context.Features.Get<IRequestLoggingFeature>();
                if (loggingFeature != null)
                {
                    loggingFeature.Enable = false;
                }
                return next(context);
            });
        }

        /// <summary>
        /// 使用反向代理中间件
        /// </summary>
        /// <param name="app"></param> 
        /// <returns></returns>
        public static IApplicationBuilder UseHttpReverseProxy(this IApplicationBuilder app)
        {
            var middleware = app.ApplicationServices.GetRequiredService<HttpReverseProxyMiddleware>();
            return app.Use(next => context => middleware.InvokeAsync(context, next));
        }
    }
}
