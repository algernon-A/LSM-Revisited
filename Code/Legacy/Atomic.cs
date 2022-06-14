using System.Threading;

namespace LoadingScreenMod
{
	internal sealed class Atomic<T>
	{
		private T slot;

		private readonly object sync = new object();

		private bool set;

		internal void Set(T item)
		{
			lock (sync)
			{
				slot = item;
				set = true;
				Monitor.Pulse(sync);
			}
		}

		internal T Get()
		{
			lock (sync)
			{
				while (!set)
				{
					Monitor.Wait(sync);
				}
				set = false;
				return slot;
			}
		}
	}
}
