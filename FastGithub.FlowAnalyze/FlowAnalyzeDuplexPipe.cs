using System.IO.Pipelines;

namespace FastGithub.FlowAnalyze
{
    sealed class FlowAnalyzeDuplexPipe : DuplexPipeStreamAdapter<FlowAnalyzeStream>
    {
        public FlowAnalyzeDuplexPipe(IDuplexPipe transport, IFlowAnalyzer flowAnalyzer) :
            base(transport, stream => new FlowAnalyzeStream(stream, flowAnalyzer))
        {
        }
    }
}
