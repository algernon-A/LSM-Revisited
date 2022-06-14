using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LoadingScreenMod
{
	internal sealed class ByPatterns
	{
		private readonly List<Regex> patterns = new List<Regex>(1);

		public bool Matches(string name)
		{
			for (int i = 0; i < patterns.Count; i++)
			{
				Regex regex = patterns[i];
				if (regex == null || regex.IsMatch(name))
				{
					return true;
				}
			}
			return false;
		}

		public void AddPattern(string pattern, bool ic)
		{
			if (pattern == "^.*$")
			{
				patterns.Insert(0, null);
			}
			else
			{
				patterns.Add(ic ? new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) : new Regex(pattern));
			}
		}
	}
}
