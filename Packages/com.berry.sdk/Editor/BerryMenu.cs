#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BerrySdk.EditorTools
{
    public static class BerryMenu
    {
        [MenuItem("Berry/Create berry-game.json")]
        public static void CreateManifest()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "berry-game.json");
            if (File.Exists(path)) { EditorUtility.DisplayDialog("Berry", "berry-game.json already exists next to Assets.", "OK"); return; }
            string id = Application.productName.ToLowerInvariant().Replace(' ', '-');
            string json = "{\n  \"schemaVersion\": 1,\n  \"id\": \"" + id + "\",\n  \"displayName\": \"" + Application.productName + "\",\n  \"version\": \"" + Application.version + "\",\n"
                + "  \"executable\": \"" + Application.productName + ".exe\",\n  \"maxPlayers\": 4,\n  \"connect\": [\"lan\", \"vlan\", \"ip\"],\n"
                + "  \"joinArgs\": [\"-connect\", \"{host}:{port}\"],\n  \"hostArgs\": [\"-host\"],\n  \"gamePort\": 7777,\n"
                + "  \"sdk\": { \"permissions\": [\"identity\", \"room\", \"players\", \"presence\"] }\n}\n";
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Berry", "berry-game.json was created next to the Assets folder.\nEdit it, then send it to the Berry team (see docs/sdk/PUBLISHING.md).", "OK");
        }

        [MenuItem("Berry/Add Berry to scene")]
        public static void AddToScene()
        {
            if (FindExisting() != null) { EditorUtility.DisplayDialog("Berry", "This scene already has a BerryBehaviour.", "OK"); return; }
            var go = new GameObject("Berry");
            go.AddComponent<BerryBehaviour>();
            Undo.RegisterCreatedObjectUndo(go, "Add Berry");
            Selection.activeGameObject = go;
        }

        private static BerryBehaviour FindExisting()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<BerryBehaviour>();
#else
            return Object.FindObjectOfType<BerryBehaviour>();
#endif
        }
    }
}
#endif
