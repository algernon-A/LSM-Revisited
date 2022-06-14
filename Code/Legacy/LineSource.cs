using System;

namespace LoadingScreenMod
{
	internal sealed class LineSource : Source
	{
		private readonly Sink sink;

		private readonly Func<bool> IsLoading;

		internal LineSource(string name, int len, Func<bool> IsLoading)
			: this(new Sink(name, len), IsLoading)
		{
		}

		internal LineSource(Sink sink, Func<bool> IsLoading)
		{
			this.sink = sink;
			this.IsLoading = IsLoading;
		}

		private static string Limit(string s)
		{
			if (s.Length > 40)
			{
				return s.Substring(0, 40);
			}
			return s;
		}

		protected internal override string CreateText()
		{
			return sink.CreateText(IsLoading());
		}

		internal void Add(string s)
		{
			sink.Add(s);
		}

		internal void AddNotFound(string s)
		{
			Add(Limit(s) + Profiling.MISSING);
		}

		internal void AddFailed(string s)
		{
			Add(Limit(s) + Profiling.FAILED);
		}

		internal void AddDuplicate(string s)
		{
			Add(Limit(s) + Profiling.DUPLICATE);
		}
	}
}
