using System;
using System.Collections.Generic;
using System.Text;

namespace WyzeSense
{
    public class ByteBuffer
    {
        private Memory<byte> _buffer;

        //This is the first byte in the buffer
        private int head;

        //This is the first byte AFTER the buffer data
        private int tail;

        //max number of bytes the buffer can hold
        private int capacity;

        //Max number of bytes the buffer can grow to
        private int maxCapacity;

        //Number of bytes contained in the buffer
        public int Size
        {
            get
            {
                if (IsEmpty)
                    return 0;
                else if (tail > head)
                    return tail - head;
                else
                    return (capacity - head) + tail;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (tail == head);
            }
        }

        private Action<string> Logger;

        /// <summary>
        /// Create a new ByteBuffer
        /// </summary>
        /// <param name="Capacity">Starting capacity of data buffer</param>
        /// <param name="MaxCapacity">Max the buffer can auto grow to</param>
        public ByteBuffer(int Capacity, Action<string> Logger = null, int MaxCapacity = 4096 * 4)
        {
            head = 0;
            tail = 0;
            this.capacity = Capacity;
            this.maxCapacity = MaxCapacity;
            this._buffer = new Memory<byte>(new byte[this.capacity]);
            this.Logger = Logger;

        }

        public void Queue(Span<byte> Data)
        {
            int dataLength = Data.Length;

            while (Size + dataLength >= capacity)
            {
                Grow();
            }

            int overflowBytes = (tail + dataLength) - capacity;

            if (overflowBytes > 0)
            {
                int bytesAtEnd = dataLength - overflowBytes;

                Log(string.Format("Queue - Wrap - Data.Length:{0,5}", dataLength));

                Data.Slice(0, bytesAtEnd).CopyTo(_buffer.Slice(tail).Span);
                Data.Slice(bytesAtEnd).CopyTo(_buffer.Slice(0, overflowBytes).Span);

            }
            else
            {
                Log(string.Format("Queue - NoWr - Data.Length:{0,5}, buffer Size: {1}", dataLength, _buffer.Slice(tail).Span.Length));
                Data.CopyTo(_buffer.Slice(tail).Span);

            }
            //if (IsEmpty)
            //    head += 1;
            //Progess the Tail 
            tail = (tail + dataLength) % capacity;

        }

        public bool Peek(Span<byte> Data) => Dequeue(Data, true);
        public bool Dequeue(Span<byte> Data) => Dequeue(Data, false);
        private bool Dequeue(Span<byte> Data, bool Peek = false)
        {
            int bytesToGet = Data.Length;

            if (bytesToGet > Size)
            {
                Log($"Dequeue - Cannot retrieve {bytesToGet} bytes from {Size} bytes");
                return false;
            }

            int overflowBytes = (head + bytesToGet) - capacity;
            if (overflowBytes > 0)
            {
                int bytesAtEnd = bytesToGet - overflowBytes;
                Log(string.Format("{1} - Wrap - Retrieve:{0:5}", bytesToGet, Peek ? "Peek" : "Dequeue"));
                _buffer.Slice(head, bytesAtEnd).Span.CopyTo(Data);
                _buffer.Slice(0, overflowBytes).Span.CopyTo(Data.Slice(bytesAtEnd));

            }
            else
            {
                Log(string.Format("{1} - NoWr - Retrieve:{0,5}", bytesToGet, Peek ? "Peek" : "Dequeue"));
                _buffer.Slice(head, bytesToGet).Span.CopyTo(Data);
            }

            //Progress the head
            if (!Peek)
                head = (head + bytesToGet) % capacity;

            return true;
        }
        public bool Burn(uint Count)
        {
            if(Count > Size)
            {
                Log($"Burn - Cannot burn {Count} bytes from {Size} bytes");
                return false;
            }
             head = (int)((head + Count) % capacity);
            return true;
        }

        private void Grow()
        {
            if (capacity * 2 > maxCapacity)
                throw new OutOfMemoryException("Cannot grow past Max Capacity of " + maxCapacity);
            Log($"Grow - Pre");

            Memory<byte> newBuffer = new Memory<byte>(new byte[capacity * 2]);
            if (IsEmpty)
                _buffer = newBuffer;
            else if (tail > head)
            {
                _buffer.Slice(head, Size).CopyTo(newBuffer);
                tail = Size;
                head = 0;
                _buffer = newBuffer;
            }
            else
            {
                //Wraps
                int bytesAtEnd = capacity - head;
                _buffer.Slice(head, bytesAtEnd).CopyTo(newBuffer);
                _buffer.Slice(0, tail).CopyTo(newBuffer.Slice(bytesAtEnd));
                tail = Size;
                head = 0;
                _buffer = newBuffer;
            }
            capacity *= 2;
            Log($"Grow - Post");
        }
        private void Log(string message, bool stamp = true)
        {
            if (stamp)
                message = string.Format("[ByteBuffer] C:{0,5} S:{1,5}, H:{2,5} T:{3,5} - {4}", capacity, Size, head, tail, message);
            else
                message = "[ByteBuffer] " + message;
            Logger?.Invoke(message);
        }
    }
}
