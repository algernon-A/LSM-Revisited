using System.Collections.Generic;

namespace LoadingScreenMod
{
    internal abstract class Item
    {
        internal string packageName;

        internal string name;

        internal CustomAssetMetaData.Type type;

        internal byte usage;

        internal abstract string FullName { get; }

        internal virtual HashSet<Item> Uses => null;

        internal bool HasPackageName => !string.IsNullOrEmpty(packageName);

        internal bool Enabled => (usage & 1) != 0;

        internal bool Available => (usage & 0x10) != 0;

        internal bool Missing => (usage & 0x20) != 0;

        internal bool Used => (usage & 6) != 0;

        internal bool UsedDir
        {
            get
            {
                return (usage & 2) != 0;
            }
            set
            {
                usage |= 2;
            }
        }

        internal bool UsedInd
        {
            get
            {
                return (usage & 4) != 0;
            }
            set
            {
                usage |= 4;
            }
        }

        internal bool Failed
        {
            get
            {
                return (usage & 8) != 0;
            }
            set
            {
                usage |= 8;
            }
        }

        internal bool NameChanged
        {
            get
            {
                return (usage & 0x40) != 0;
            }
            set
            {
                usage |= 64;
            }
        }

        protected Item(string packageName, string name_Data, CustomAssetMetaData.Type type, int usage)
        {
            this.packageName = packageName;
            name = LoadingScreenModRevisited.AssetLoader.ShortName(name_Data);
            this.type = type;
            this.usage = (byte)usage;
        }

        protected Item(string fullName, CustomAssetMetaData.Type type, int usage)
        {
            int num = fullName.IndexOf('.');
            if (num >= 0)
            {
                packageName = fullName.Substring(0, num);
                name = LoadingScreenModRevisited.AssetLoader.ShortName(fullName.Substring(num + 1));
            }
            else
            {
                packageName = string.Empty;
                name = LoadingScreenModRevisited.AssetLoader.ShortName(fullName);
            }
            this.type = type;
            this.usage = (byte)usage;
        }

        internal virtual void Add(Item child)
        {
        }
    }
}
