using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OfficialCardPrefabImporter
{
    private const string ManifestPath = "Assets/Resources/Cards/official_cards.json";
    private const string BasePrefabPath = "Assets/Resources/Cards/0000_LL-bp1-001-R+.prefab";
    private const string SharedPrefabPath = "Assets/Resources/Cards/cardPrefab.prefab";

    [Serializable] private class Manifest { public CardRecord[] cards; }
    [Serializable] private class CardRecord
    {
        public int id;
        public string folder;
        public string cardNumber;
        public string name;
        public string type;
        public string imageUrl;
    }

    [MenuItem("Tools/Cards/Import Official Card Catalog")]
    public static void Import()
    {
        if (!File.Exists(ManifestPath))
            throw new FileNotFoundException("Run Tools/ImportOfficialCards.py before importing prefabs.", ManifestPath);

        Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabPath)
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (source == null) throw new InvalidOperationException("No card prefab exists to use as the shared template.");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = "cardPrefab";
        SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
        renderer.sprite = null;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(2.32f, 3.2f);
        Card card = instance.GetComponent<Card>();
        SerializedObject serializedCard = new SerializedObject(card);
        serializedCard.FindProperty("CardImage").objectReferenceValue = null;
        serializedCard.FindProperty("streamingImagePath").stringValue = string.Empty;
        serializedCard.FindProperty("officialImageUrl").stringValue = string.Empty;
        serializedCard.FindProperty("cardID").intValue = 0;
        serializedCard.FindProperty("cardName").stringValue = string.Empty;
        serializedCard.FindProperty("Type").enumValueIndex = 0;
        serializedCard.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(instance, SharedPrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);

        foreach (CardRecord record in manifest.cards)
        {
            string productFolder = "Assets/Resources/Cards/" + record.folder;
            if (AssetDatabase.IsValidFolder(productFolder)) AssetDatabase.DeleteAsset(productFolder);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Cards" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != SharedPrefabPath) AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Replaced all card prefabs with Resources/Cards/cardPrefab.prefab.");
    }

    private static int CardType(string value)
    {
        if (value.Contains("ライブ")) return (int)Card.CardType.Live - 1;
        if (value.Contains("エネルギー")) return (int)Card.CardType.Energy - 1;
        return (int)Card.CardType.Member - 1;
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
        return value;
    }
}
