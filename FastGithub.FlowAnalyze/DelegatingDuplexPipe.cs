using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace FastGithub.FlowAnalyze
{
    class DelegatingDuplexPipe<TDelegatingStream> : IDuplexPipe, IAsyncDisposable where TDelegatingStream : DelegatingStream
    {
        private bool disposed;
        private readonly object syncRoot = new();

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public DelegatingDuplexPipe(IDuplexPipe duplexPipe, Func<Stream, TDelegatingStream> delegatingStreamFactory) :
            this(duplexPipe, new StreamPipeReaderOptions(leaveOpen: true), new StreamPipeWriterOptions(leaveOpen: true), delegatingStreamFactory)
        {
        }

        public DelegatingDuplexPipe(IDuplexPipe duplexPipe, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, TDelegatingStream> delegatingStreamFactory)
        {
            var delegatingStream = delegatingStreamFactory(duplexPipe.AsStream());
            this.Input = PipeReader.Create(delegatingStream, readerOptions);
            this.Output = PipeWriter.Create(delegatingStream, writerOptions);
        }

        public virtual async ValueTask DisposeAsync()
        {
            lock (this.syncRoot)
            {
                if (this.disposed == true)
                {
                    return;
                }
                this.disposed = true;
            }

            await this.Input.CompleteAsync();
            await this.Output.CompleteAsync();
        }
    }
}