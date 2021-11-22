using System.IO.Pipelines;

namespace FastGithub.FlowAnalyze
{
    sealed class FlowAnalyzeDuplexPipe : DelegatingDuplexPipe<FlowAnalyzeStream>
    {
        public FlowAnalyzeDuplexPipe(IDuplexPipe duplexPipe, IFlowAnalyzer flowAnalyzer) :
            base(duplexPipe, stream => new FlowAnalyzeStream(stream, flowAnalyzer))
        {
        }
    }
}
