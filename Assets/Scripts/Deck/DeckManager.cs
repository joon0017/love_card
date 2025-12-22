using System.IO;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    public Card[] AllCards;        // Master list
    public Card[] InDeck;          // Active deck

    private string filePath;
    private const string encryptionKey = "1234567890123456";

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "deck.dat");

        LoadDeck();
        ShuffleDeck();
    }

    void LoadDeck()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Deck file not found. Creating default deck.");
            CreateDefaultDeck();
            SaveDeck();
            return;
        }

        string encrypted = File.ReadAllText(filePath);
        string json = CryptoUtil.Decrypt(encrypted, encryptionKey);

        DeckData data = JsonUtility.FromJson<DeckData>(json);

        InDeck = new Card[data.cardIds.Length];
        for (int i = 0; i < data.cardIds.Length; i++)
        {
            InDeck[i] = AllCards[data.cardIds[i]];
        }
    }

    void SaveDeck()
    {
        DeckData data = new DeckData();
        data.cardIds = new int[InDeck.Length];

        for (int i = 0; i < InDeck.Length; i++)
        {
            data.cardIds[i] = System.Array.IndexOf(AllCards, InDeck[i]);
        }

        string json = JsonUtility.ToJson(data);
        string encrypted = CryptoUtil.Encrypt(json, encryptionKey);

        File.WriteAllText(filePath, encrypted);
    }

    void CreateDefaultDeck()
    {
        InDeck = new Card[AllCards.Length];
        for (int i = 0; i < AllCards.Length; i++)
            InDeck[i] = AllCards[i];
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < InDeck.Length; i++)
        {
            int r = Random.Range(i, InDeck.Length);
            (InDeck[i], InDeck[r]) = (InDeck[r], InDeck[i]);
        }
    }
}
