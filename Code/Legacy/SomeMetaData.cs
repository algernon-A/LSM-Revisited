using ColossalFramework.Packaging;

namespace LoadingScreenMod
{
	internal struct SomeMetaData
	{
		internal Package.Asset userDataRef;

		internal string name;

		internal SomeMetaData(Package.Asset u, string n)
		{
			userDataRef = u;
			name = n;
		}
	}
}
