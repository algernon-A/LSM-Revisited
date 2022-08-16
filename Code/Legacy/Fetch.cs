using System.Collections.Generic;

namespace LoadingScreenMod
{
    internal static class Fetch<T> where T : PrefabInfo
    {
        private static Dictionary<string, PrefabCollection<T>.PrefabData> prefabDict;

        internal static Dictionary<string, PrefabCollection<T>.PrefabData> PrefabDict
        {
            get
            {
                if (prefabDict == null)
                {
                    prefabDict = (Dictionary<string, PrefabCollection<T>.PrefabData>)Util.GetStatic(typeof(PrefabCollection<T>), "m_prefabDict");
                }
                return prefabDict;
            }
        }

        internal static void Dispose()
        {
            prefabDict = null;
        }
    }
}
