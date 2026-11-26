using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One time helper: puts Boss Last Words on the boss of each level, with its own lines. Delete after it has run.
public static class AddBossLastWords
{
    [MenuItem("Tools/Dragon's Toll/Add boss last words (one time)")]
    public static void AddAll()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("Save the open scene first.");
            return;
        }
        string startScene = EditorSceneManager.GetActiveScene().path;

        AddToBoss("Assets/Scenes/Level1_Woods_Skeleton.unity", "Skeleton BOSS", new Color(0.92f, 0.92f, 0.85f), new string[]
        {
            "Ha ha ha... am I gone?? Bones never stay broken for long...",
            "Heh heh heh... I'm not gone, little mage. I'm only going ahead to wait with the Dragon.",
            "Rattle, rattle... can you hear that? It's my master's wings.",
            "You won the woods... but the Dragon keeps my bones. We will meet again.",
            "Snap me all you like. The Dragon will put me back together!",
            "Ha ha ha! Enjoy it, mage. The Dragon's lair has room for one more skeleton... YOURS.",
        });

        AddToBoss("Assets/Scenes/Level2_Woods_FireImp.unity", "FireImp BOSS", new Color(1f, 0.6f, 0.2f), new string[]
        {
            "Hee hee hee... am I gone?? Embers never really go out...",
            "You put out MY fire... but the Dragon breathes a much bigger one!",
            "Rain won't save you where I'm going. The Dragon's flames are waiting!",
            "Hee hee! I'm only smoke now... and smoke drifts to the Dragon's lair.",
            "Sizzle, sizzle... I'll be warming my hands on the Dragon's breath. See you there!",
            "Ha ha! Storm all you want, mage. Nothing puts out a Dragon.",
        });

        AddToBoss("Assets/Scenes/Level3_Woods_StoneGolem.unity", "StoneGolem BOSS", new Color(0.75f, 0.8f, 0.9f), new string[]
        {
            "Ha... ha... ha... am I gone?? A mountain never really falls...",
            "Stone cracks, little mage... but the Dragon will make me whole again.",
            "Crumble me if you like. Every pebble rolls toward the Dragon.",
            "You broke my body. The Dragon will give me a stronger one.",
            "Rocks remember, mage... and we will remember YOU in the Dragon's cave.",
            "This is not the end. The Dragon is waking... and I will stand beside it.",
        });

        EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
        Debug.Log("Boss last words added to all three bosses.");
    }

    static void AddToBoss(string scenePath, string bossName, Color textColor, string[] bossLines)
    {
        Scene levelScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject bossObject = null;
        foreach (Health oneHealth in Object.FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (oneHealth.gameObject.name == bossName)
            {
                bossObject = oneHealth.gameObject;
            }
        }
        if (bossObject == null)
        {
            Debug.LogError("Could not find " + bossName + " in " + scenePath);
            return;
        }
        BossLastWords wordsScript = bossObject.GetComponent<BossLastWords>();
        if (wordsScript == null)
        {
            wordsScript = Undo.AddComponent<BossLastWords>(bossObject);
        }
        wordsScript.lastWords = bossLines;
        wordsScript.textColor = textColor;
        EditorUtility.SetDirty(wordsScript);
        PrefabUtility.RecordPrefabInstancePropertyModifications(wordsScript);
        EditorSceneManager.MarkSceneDirty(levelScene);
        EditorSceneManager.SaveScene(levelScene);
        Debug.Log("Added last words to " + bossName);
    }
}
