using ColossalFramework.Packaging;

namespace LoadingScreenMod
{
    internal sealed class Fixes : DetourUtility<Fixes>
    {
        private Fixes()
        {
            init(typeof(BuildConfig), "ResolveCustomAssetName", typeof(LoadingScreenModRevisited.CustomDeserializer), "ResolveCustomAssetName");
            init(typeof(PackageReader), "ReadByteArray", typeof(MemReader), "DreadByteArray");
        }
    }
}
