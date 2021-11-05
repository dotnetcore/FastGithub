using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.FlowAnalyze
{

    sealed class FlowAnalyzeStream : Stream
    {
        private readonly Stream inner;
        private readonly IFlowAnalyzer flowAnalyzer;

        public FlowAnalyzeStream(Stream inner, IFlowAnalyzer flowAnalyzer)
        {
            this.inner = inner;
            this.flowAnalyzer = flowAnalyzer;
        }

        public override bool CanRead
        {
            get
            {
                return inner.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return inner.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return inner.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return inner.Length;
            }
        }

        public override long Position
        {
            get
            {
                return inner.Position;
            }

            set
            {
                inner.Position = value;
            }
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = inner.Read(buffer, offset, count);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override int Read(Span<byte> destination)
        {
            int read = inner.Read(destination);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            int read = await inner.ReadAsync(destination, cancellationToken);
            this.flowAnalyzer.OnFlow(FlowType.Read, read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, count);
            inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, source.Length);
            inner.Write(source);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, count);
            return inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            this.flowAnalyzer.OnFlow(FlowType.Wirte, source.Length);
            return inner.WriteAsync(source, cancellationToken);
        }


        // The below APM methods call the underlying Read/WriteAsync methods which will still be logged.
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }
    }
}
