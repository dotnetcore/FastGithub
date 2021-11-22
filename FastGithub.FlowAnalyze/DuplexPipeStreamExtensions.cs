using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.FlowAnalyze
{
    static class DuplexPipeStreamExtensions
    {
        public static Stream AsStream(this IDuplexPipe duplexPipe, bool throwOnCancelled = false)
        {
            return new DuplexPipeStream(duplexPipe, throwOnCancelled);
        }

        private class DuplexPipeStream : Stream
        {
            private readonly PipeReader input;
            private readonly PipeWriter output;
            private readonly bool throwOnCancelled;
            private volatile bool cancelCalled;

            public DuplexPipeStream(IDuplexPipe duplexPipe, bool throwOnCancelled = false)
            {
                this.input = duplexPipe.Input;
                this.output = duplexPipe.Output;
                this.throwOnCancelled = throwOnCancelled;
            }

            public void CancelPendingRead()
            {
                this.cancelCalled = true;
                this.input.CancelPendingRead();
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ValueTask<int> vt = ReadAsyncInternal(new Memory<byte>(buffer, offset, count), default);
                return vt.IsCompleted ?
                    vt.Result :
                    vt.AsTask().GetAwaiter().GetResult();
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                return ReadAsyncInternal(destination, cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
            }

            public override async Task WriteAsync(byte[]? buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await this.output.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                await this.output.WriteAsync(source, cancellationToken);
            }

            public override void Flush()
            {
                FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                await this.output.FlushAsync(cancellationToken);
            }

            [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
            private async ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken)
            {
                while (true)
                {
                    var result = await this.input.ReadAsync(cancellationToken);
                    var readableBuffer = result.Buffer;
                    try
                    {
                        if (this.throwOnCancelled && result.IsCanceled && this.cancelCalled)
                        {
                            // Reset the bool
                            this.cancelCalled = false;
                            throw new OperationCanceledException();
                        }

                        if (!readableBuffer.IsEmpty)
                        {
                            // buffer.Count is int
                            var count = (int)Math.Min(readableBuffer.Length, destination.Length);
                            readableBuffer = readableBuffer.Slice(0, count);
                            readableBuffer.CopyTo(destination.Span);
                            return count;
                        }

                        if (result.IsCompleted)
                        {
                            return 0;
                        }
                    }
                    finally
                    {
                        this.input.AdvanceTo(readableBuffer.End, readableBuffer.End);
                    }
                }
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
}
