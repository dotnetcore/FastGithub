using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.FlowAnalyze
{
    sealed class FlowAnalyzeStream : DelegatingStream
    {
        private readonly IFlowAnalyzer flowAnalyzer;

        public FlowAnalyzeStream(Stream inner, IFlowAnalyzer flowAnalyzer)
            : base(inner)
        {
            this.flowAnalyzer = flowAnalyzer;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override int Read(Span<byte> destination)
        {
            int read = base.Read(destination);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            int read = await base.ReadAsync(destination, cancellationToken);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, count);
            base.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, source.Length);
            base.Write(source);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, count);
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, source.Length);
            return base.WriteAsync(source, cancellationToken);
        }
    }
}
