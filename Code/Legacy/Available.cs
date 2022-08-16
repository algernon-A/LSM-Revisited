using System.Collections.Generic;
using ColossalFramework.Packaging;

namespace LoadingScreenMod
{
    internal sealed class Available : Item
    {
        private HashSet<Item> uses;

        internal Package.Asset mainAssetRef;

        internal override string FullName => mainAssetRef.fullName;

        internal override HashSet<Item> Uses => uses;

        internal Available(Package.Asset mainAssetRef, CustomAssetMetaData.Type type, bool enabled, bool useddir)
            : base(mainAssetRef.package.packageName, mainAssetRef.name, type, 0x10 | (enabled ? 1 : 0) | (useddir ? 2 : 0))
        {
            this.mainAssetRef = mainAssetRef;
        }

        internal override void Add(Item child)
        {
            if (uses != null)
            {
                uses.Add(child);
                return;
            }
            uses = new HashSet<Item> { child };
        }
    }
}
