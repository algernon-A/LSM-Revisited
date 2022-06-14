using System.Collections.Generic;

namespace LoadingScreenMod
{
	internal sealed class ByNames
	{
		private readonly HashSet<string> names = new HashSet<string>();

		public bool Matches(string name)
		{
			return names.Contains(name);
		}

		public void AddName(string name)
		{
			names.Add(name);
		}
	}
}
