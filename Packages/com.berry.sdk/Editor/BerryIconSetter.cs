#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BerrySdk.EditorTools
{
    [InitializeOnLoad]
    public static class BerryIconSetter
    {
        private const string IconPath = "Packages/com.berry.sdk/Editor/Icons/berry-icon.png";

        static BerryIconSetter()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            try
            {
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
                if (icon == null) return;
                foreach (string guid in AssetDatabase.FindAssets("BerryBehaviour t:MonoScript", new[] { "Packages/com.berry.sdk", "Assets" }))
                {
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                    if (script != null && script.name == "BerryBehaviour" && EditorGUIUtility.GetIconForObject(script) != icon)
                        EditorGUIUtility.SetIconForObject(script, icon);
                }
            }
            catch (System.Exception) {  }
        }
    }
}
#endif
