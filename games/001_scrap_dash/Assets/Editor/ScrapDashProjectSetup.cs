#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ScrapDash.Editor
{
    [InitializeOnLoad]
    public static class ScrapDashProjectSetup
    {
        private const string IconPath = "Assets/Art/scrap-dash-icon.png";

        static ScrapDashProjectSetup()
        {
            EditorApplication.delayCall += Apply;
        }

        public static void Apply()
        {
            PlayerSettings.companyName = "SWIR";
            PlayerSettings.productName = "SCRAP DASH";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            ApplyGameIcon();

            var settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settingsAssets.Length == 0) return;

            var serialized = new SerializedObject(settingsAssets[0]);
            var activeInputHandler = serialized.FindProperty("activeInputHandler");
            if (activeInputHandler != null && activeInputHandler.intValue != 1)
            {
                activeInputHandler.intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                Debug.Log("SCRAP DASH: enabled the new Unity Input System backend.");
            }
        }

        private static void ApplyGameIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;

            var sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
            if (sizes.Length == 0) return;

            var icons = new Texture2D[sizes.Length];
            for (var i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icons);
        }
    }
}
#endif
