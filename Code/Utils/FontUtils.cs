using ColossalFramework.UI;
using System.Linq;
using UnityEngine;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Static utilities class for handling fonts.
    /// </summary>
    public static class FontUtils
    {
        /// <summary>
        /// Regular sans-serif font.
        /// </summary>
        public static UIFont Regular
        {
            get
            {
                if (_regular == null)
                {
                    _regular = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Regular");
                }
                return _regular;
            }
        }
        private static UIFont _regular;
    }
}