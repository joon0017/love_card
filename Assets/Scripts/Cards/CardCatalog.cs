using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CardDefinition
{
    public int id;
    public string cardNumber;
    public string name;
    public string name_kr;
    public string type;
    public string type_kr;
    public string imageUrl;

    public Card.CardType CardType
    {
        get
        {
            if (!string.IsNullOrEmpty(type) && type.Contains("ライブ")) return Card.CardType.Live;
            if (!string.IsNullOrEmpty(type) && type.Contains("エネルギー")) return Card.CardType.Energy;
            return Card.CardType.Member;
        }
    }
}

public static class CardCatalog
{
    [Serializable] private class CatalogData { public CardDefinition[] cards; }
    private static CardDefinition[] cards;
    private static Dictionary<int, CardDefinition> byId;

    public static CardDefinition[] All
    {
        get { EnsureLoaded(); return cards; }
    }

    public static bool TryGet(int id, out CardDefinition definition)
    {
        EnsureLoaded();
        return byId.TryGetValue(id, out definition);
    }

    public static Card CreateCard(Card sharedPrefab, CardDefinition definition, Vector3 position, Quaternion rotation)
    {
        if (sharedPrefab == null || definition == null) return null;
        Card card = UnityEngine.Object.Instantiate(sharedPrefab, position, rotation);
        card.Configure(definition);
        return card;
    }

    private static void EnsureLoaded()
    {
        if (cards != null) return;
        TextAsset asset = Resources.Load<TextAsset>("Cards/official_cards");
        CatalogData data = asset != null ? JsonUtility.FromJson<CatalogData>(asset.text) : null;
        cards = data?.cards ?? new CardDefinition[0];
        byId = cards.GroupBy(card => card.id).ToDictionary(group => group.Key, group => group.First());
        if (cards.Length == 0) Debug.LogError("No card definitions found in Resources/Cards/official_cards.json");
    }
}
