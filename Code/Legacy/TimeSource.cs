namespace LoadingScreenMod
{
	internal sealed class TimeSource : Source
	{
		protected internal override string CreateText()
		{
			return Profiling.TimeString(Profiling.Millis);
		}
	}
}
