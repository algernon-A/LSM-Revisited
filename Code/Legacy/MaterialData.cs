using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class MaterialData
	{
		internal readonly Material material;

		internal readonly int textureCount;

		internal MaterialData(Material m, int count)
		{
			material = m;
			textureCount = count;
		}
	}
}
