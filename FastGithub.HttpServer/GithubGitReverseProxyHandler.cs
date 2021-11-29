using FastGithub.DomainResolve;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// github的git代理处理者
    /// </summary>
    sealed class GithubGitReverseProxyHandler : TcpReverseProxyHandler
    {
        /// <summary>
        /// github的git代理处理者
        /// </summary>
        /// <param name="domainResolver"></param>
        public GithubGitReverseProxyHandler(IDomainResolver domainResolver)
            : base(domainResolver, new("github.com", 9418))
        {
        }
    }
}
