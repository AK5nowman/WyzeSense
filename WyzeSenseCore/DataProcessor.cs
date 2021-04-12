using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    internal class DataProcessor
    {
        // Fields and Properties
        protected CancellationToken CancelReads;
        protected CancellationToken CancelWrites;
        protected readonly Channel<Action> Messages;
        protected readonly Thread Thread;

        public DataProcessor(
            CancellationToken cancelReads = default(CancellationToken),
            CancellationToken cancelWrites = default(CancellationToken))
        {
            // Initialize the channel
            this.CancelReads = cancelReads;
            this.CancelWrites = cancelWrites;
            this.Messages = Channel.CreateUnbounded<Action>();

            Thread = new Thread(this.Dequeue);
            Thread.Start();
        }


        public void Queue(Action WyzeAction)
        {
            if (!this.CancelWrites.IsCancellationRequested)
                this.Messages.Writer.TryWrite(WyzeAction);
        }

        private async void Dequeue()
        {
            while (!this.CancelReads.IsCancellationRequested)
            {
                try
                {
                    var msg = await this.Messages.Reader.ReadAsync(this.CancelReads);
                    msg?.Invoke();
                }
                catch (OperationCanceledException oce) { }
            }
        }

        public void Shutdown()
        {
            this.CancelWrites = new CancellationToken(true);
            this.Messages.Reader.Completion.Wait();
            this.CancelReads = new CancellationToken(true);
        }
    }
}
