using System.Collections.Generic;
using System.Threading;

namespace LoadingScreenMod
{
    internal sealed class ConcurrentQueue<T>
    {
        private readonly Queue<T> queue;

        private readonly object sync = new object();

        private bool completed;

        internal ConcurrentQueue(int capacity)
        {
            queue = new Queue<T>(capacity);
        }

        internal void Enqueue(T item)
        {
            lock (sync)
            {
                queue.Enqueue(item);
                Monitor.Pulse(sync);
            }
        }

        internal bool Dequeue(out T result)
        {
            lock (sync)
            {
                while (!completed && queue.Count == 0)
                {
                    Monitor.Wait(sync);
                }
                if (queue.Count > 0)
                {
                    result = queue.Dequeue();
                    return true;
                }
            }
            result = default(T);
            return false;
        }

        internal T[] DequeueAll()
        {
            lock (sync)
            {
                if (queue.Count == 0)
                {
                    return null;
                }
                T[] result = queue.ToArray();
                queue.Clear();
                return result;
            }
        }

        internal void SetCompleted()
        {
            lock (sync)
            {
                completed = true;
                Monitor.PulseAll(sync);
            }
        }
    }
}
