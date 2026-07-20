using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OfficialCardPrefabImporter
{
    private const string ManifestPath = "Assets/Resources/Cards/official_cards.json";
    private const string BasePrefabPath = "Assets/Resources/Cards/0000_LL-bp1-001-R+.prefab";

    [Serializable] private class Manifest { public CardRecord[] cards; }
    [Serializable] private class CardRecord
    {
        public int id;
        public string folder;
        public string cardNumber;
        public string name;
        public string type;
        public string streamingImagePath;
    }

    [MenuItem("Tools/Cards/Import Downloaded Official Cards")]
    public static void Import()
    {
        if (!File.Exists(ManifestPath))
            throw new FileNotFoundException("Run Tools/ImportOfficialCards.py before importing prefabs.", ManifestPath);

        Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null) throw new InvalidOperationException("Base card prefab is missing: " + BasePrefabPath);

        int created = 0;
        foreach (CardRecord record in manifest.cards)
        {
            string folder = "Assets/Resources/Cards/" + record.folder;
            EnsureFolder(folder);
            string safeNumber = SanitizeFileName(record.cardNumber);
            string prefabPath = $"{folder}/{record.id:D5}_{safeNumber}.prefab";
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = $"{record.id:D5}_{record.cardNumber}";
            instance.GetComponent<SpriteRenderer>().sprite = null;

            Card card = instance.GetComponent<Card>();
            SerializedObject serializedCard = new SerializedObject(card);
            serializedCard.FindProperty("CardImage").objectReferenceValue = null;
            serializedCard.FindProperty("streamingImagePath").stringValue = record.streamingImagePath;
            serializedCard.FindProperty("cardID").intValue = record.id;
            serializedCard.FindProperty("cardName").stringValue = record.name;
            serializedCard.FindProperty("Type").enumValueIndex = CardType(record.type);
            serializedCard.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            created++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Imported {created} official card prefabs into categorized Resources/Cards folders.");
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
