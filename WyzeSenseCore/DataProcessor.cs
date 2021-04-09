using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WyzeSense
{
    public class DataProcessor
    {
        // Fields and Properties
        protected CancellationToken CancelReads;
        protected CancellationToken CancelWrites;
        protected readonly Channel<Message> Messages;
        protected readonly Action<DateTime, byte[]> Process;
        protected readonly Thread Thread;

        public DataProcessor(
            Action<DateTime, byte[]> process,
            CancellationToken cancelReads = default(CancellationToken),
            CancellationToken cancelWrites = default(CancellationToken))
        {
            // Initialize the channel
            this.CancelReads = cancelReads;
            this.CancelWrites = cancelWrites;
            this.Messages = Channel.CreateUnbounded<Message>();
            this.Process = process;

            Thread = new Thread(this.Dequeue);
            Thread.Start();
        }


        public void Queue(byte[] packet)
        {
            if (!this.CancelWrites.IsCancellationRequested)
                this.Messages.Writer.TryWrite(new Message
                {
                    QueueTime = DateTime.Now,
                    Data = packet
                });
        }

        private async void Dequeue()
        {
            while (!this.CancelReads.IsCancellationRequested)
            {
                var msg = await this.Messages.Reader.ReadAsync(this.CancelReads);
                if (msg != null)
                {
                    this.Process(msg.QueueTime, msg.Data);
                }
                  
            }
        }

        public void Shutdown()
        {
            this.CancelWrites = new CancellationToken(true);
            this.Messages.Reader.Completion.Wait();
            this.CancelReads = new CancellationToken(true);
        }

        protected class Message
        {
            public DateTime QueueTime;
            public byte[] Data;
        }
    }
}
