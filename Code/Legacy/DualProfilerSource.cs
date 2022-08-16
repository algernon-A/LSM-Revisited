using AlgernonCommons.Translation;
using ColossalFramework;

namespace LoadingScreenMod
{
    internal sealed class DualProfilerSource : Source
    {
        private readonly Source scenes;

        private readonly LineSource assets;

        private readonly Sink sink;

        private readonly string name;

        private readonly string failedStr = ' ' + Translations.Translate("FAILED") + ' ';

        private readonly string missingStr = ' ' + Translations.Translate("MISSING") + ' ';

        private readonly string duplicatesStr = ' ' + Translations.Translate("DUPLICATE") + ' ';

        private int state;

        private int failed;

        private int duplicate;

        private int notFound;

        internal DualProfilerSource(string name, int len)
        {
            sink = new Sink(name, len);
            this.name = name;
            scenes = new ProfilerSource(Singleton<LoadingManager>.instance.m_loadingProfilerScenes, sink);
            assets = new LineSource(sink, () => true);
        }

        protected internal override string CreateText()
        {
            string result = (state == 1) ? assets.CreateText() : scenes.CreateText();
            if (state == 0 && LoadingScreenModRevisited.LevelLoader.s_assetLoadingStarted)
            {
                state = 1;
                return result;
            }
            if (state == 1 && LoadingScreenModRevisited.LevelLoader.s_assetsFinished)
            {
                state = 2;
            }
            return result;
        }

        internal void Add(string s)
        {
            assets.Add(s);
        }

        internal void CustomAssetNotFound(string n)
        {
            notFound++;
            AdjustName();
            assets.AddNotFound(n);
        }

        internal void CustomAssetFailed(string n)
        {
            failed++;
            AdjustName();
            assets.AddFailed(n);
        }

        internal void CustomAssetDuplicate(string n)
        {
            duplicate++;
            AdjustName();
            assets.AddDuplicate(n);
        }

        private void AdjustName()
        {
            string text = ((failed == 0) ? string.Empty : (failed + failedStr));
            string text2 = ((notFound == 0) ? string.Empty : (notFound + missingStr));
            string text3 = ((duplicate == 0) ? string.Empty : (duplicate + duplicatesStr));
            sink.Name = name + " " + text + text2 + text3;
        }
    }
}
