/**
 * This plugin adds a "Rate It" checkbox in the right-sidebar of the character creation screen (Maker).
 * When the checkbox is toggled, a panel appears with buttons to rate the currently loaded character card.
 * Based on the rating selected, the character card will be moved to the appropriate folder:
 * \Koikatsu\UserData\chara\female\{RatingType}
 * 
 * TODO: Path should be configable.
 */

using BepInEx;
using BepInEx.Configuration;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using UniRx;
using UnityEngine;

namespace RatingPlugin
{
    [BepInPlugin(GUID, "Rating Plugin", Version)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
    public class RatingPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.soapproject.koikatsu.ratingplugin";
        public const string Version = "1.0.0";
        public static ConfigEntry<string> RatingTypesConfig;

        private void Awake()
        {
            // bind default config
            RatingTypesConfig = Config.Bind("General", "RatingTypes", "Like,Dislike,MissingMod,OtherIssue", "List of rating types separated by commas.");

            // regiist event which Draw checkbox
            MakerAPI.RegisterCustomSubCategories += OnMakerSceneLoaded;

            // init Panel
            var obj = new GameObject("PanelGUI");
            PanelGUI.Instance = obj.AddComponent<PanelGUI>();
            DontDestroyOnLoad(obj);
        }

        // Draw a checkbox in the right-sidebar
        private void OnMakerSceneLoaded(object sender, RegisterSubCategoriesEvent args)
        {
            var toggle = args.AddSidebarControl(new SidebarToggle("Rate It", false, this));
            toggle.ValueChanged.Subscribe(value =>
            {
                togglePanelGUI(value);
            });
        }

        private void togglePanelGUI(bool toggleState)
        {
            if (!toggleState)
                PanelGUI.Hide();
            else
            {
                if (MakerAPI.InsideAndLoaded)
                    PanelGUI.Show();
            }
        }

        private void OnDestroy()
        {
            MakerAPI.RegisterCustomSubCategories -= OnMakerSceneLoaded;
        }
    }
}