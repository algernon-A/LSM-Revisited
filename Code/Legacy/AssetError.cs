using System;
using ColossalFramework.Packaging;

namespace LoadingScreenMod
{
    internal sealed class AssetError<V> : IEquatable<AssetError<V>>
    {
        internal readonly Package package;

        internal readonly string checksum;

        internal readonly V value;

        internal AssetError(Package p, string c, V v)
        {
            package = p;
            checksum = c;
            value = v;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AssetError<V>);
        }

        public bool Equals(AssetError<V> other)
        {
            if (other != null)
            {
                return checksum == other.checksum;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return checksum.GetHashCode();
        }

        internal AssetError<U> Map<U>(Func<V, U> m)
        {
            return new AssetError<U>(package, checksum, m(value));
        }
    }
}
