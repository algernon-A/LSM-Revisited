using System;

namespace LoadingScreenMod
{
	public abstract class Instance<T>
	{
		private static T inst;

		internal static T instance
		{
			get
			{
				return inst;
			}
			set
			{
				inst = value;
			}
		}

		internal static bool HasInstance => inst != null;

		internal static T Create()
		{
			if (inst == null)
			{
				inst = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
			}
			return inst;
		}
	}
}
