namespace LoadingScreenMod
{
    internal sealed class Missing : Item
    {
        private readonly string fullName;

        internal override string FullName => fullName;

        internal Missing(string fullName, CustomAssetMetaData.Type type, bool useddir = false)
            : base(fullName, type, 0x20 | (useddir ? 2 : 0))
        {
            this.fullName = fullName;
        }
    }
}
