using System.Collections.Generic;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class AtlasObj
	{
		internal Package.Asset asset;

		internal UITextureAtlas atlas;

		internal byte[] bytes;

		internal int width;

		internal int height;

		internal List<UITextureAtlas.SpriteInfo> sprites;

		internal TextureFormat format;
	}
}
