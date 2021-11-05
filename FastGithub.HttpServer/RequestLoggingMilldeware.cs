using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// 请求日志中间件
    /// </summary>
    sealed class RequestLoggingMiddleware
    {
        private readonly ILogger<RequestLoggingMiddleware> logger;

        /// <summary>
        /// 请求日志中间件
        /// </summary>
        /// <param name="logger"></param>
        public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var feature = new RequestLoggingFeature();
            context.Features.Set<IRequestLoggingFeature>(feature);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();
            }

            if (feature.Enable == false)
            {
                return;
            }

            var request = context.Request;
            var response = context.Response;
            var message = $"{request.Method} {request.Scheme}://{request.Host}{request.Path} responded {response.StatusCode} in {stopwatch.Elapsed.TotalMilliseconds} ms";
            var exception = context.GetForwarderErrorFeature()?.Exception;
            if (exception == null)
            {
                this.logger.LogInformation(message);
            }
            else if (IsError(exception))
            {
                this.logger.LogError($"{message}{Environment.NewLine}{exception}");
            }
            else
            {
                this.logger.LogWarning($"{message}{Environment.NewLine}{GetMessage(exception)}");
            }
        }

        /// <summary>
        /// 是否为错误
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private static bool IsError(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            if (HasInnerException<ConnectionAbortedException>(exception))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否有内部异常异常
        /// </summary>
        /// <typeparam name="TInnerException"></typeparam>
        /// <param name="exception"></param>
        /// <returns></returns>
        private static bool HasInnerException<TInnerException>(Exception exception) where TInnerException : Exception
        {
            var inner = exception.InnerException;
            while (inner != null)
            {
                if (inner is TInnerException)
                {
                    return true;
                }
                inner = inner.InnerException;
            }
            return false;
        }

        /// <summary>
        /// 获取异常信息
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private static string GetMessage(Exception exception)
        {
            var ex = exception;
            var builder = new StringBuilder();

            while (ex != null)
            {
                var type = ex.GetType();
                builder.Append(type.Namespace).Append('.').Append(type.Name).Append(": ").AppendLine(ex.Message);
                ex = ex.InnerException;
            }
            return builder.ToString();
        }

        private class RequestLoggingFeature : IRequestLoggingFeature
        {
            public bool Enable { get; set; } = true;
        }
    }
}
