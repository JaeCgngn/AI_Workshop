#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NpcWorkshopSceneSetup
{
    const string MenuPath = "AI Workshop/Setup NPC Scene";

    [MenuItem(MenuPath)]
    public static void SetupNpcScene()
    {
        EnsureTagExists("Player");
        NpcWorkshopBuilder.BuildIfMissing();

        var player = GameObject.Find("Player");
        if (player != null)
        {
            player.tag = "Player";
            Selection.activeGameObject = player;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("NPC scene setup complete. Ground, Player, and NPC are ready.");
    }

    static void EnsureTagExists(string tag)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0)
            return;

        var tagManager = new SerializedObject(asset[0]);
        var tags = tagManager.FindProperty("tags");

        for (var i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
