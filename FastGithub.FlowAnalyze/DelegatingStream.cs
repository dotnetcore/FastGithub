using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.FlowAnalyze
{
    abstract class DelegatingStream : Stream
    {
        protected Stream Inner { get; }

        public DelegatingStream(Stream inner)
        {
            this.Inner = inner;
        }

        public override bool CanRead
        {
            get
            {
                return this.Inner.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.Inner.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.Inner.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return this.Inner.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.Inner.Position;
            }

            set
            {
                this.Inner.Position = value;
            }
        }

        public override void Flush()
        {
            this.Inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return this.Inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.Inner.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> destination)
        {
            return this.Inner.Read(destination);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.Inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            return this.Inner.ReadAsync(destination, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.Inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.Inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.Inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            this.Inner.Write(source);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.Inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return this.Inner.WriteAsync(source, cancellationToken);
        }

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
