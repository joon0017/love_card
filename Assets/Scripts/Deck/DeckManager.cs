using System.IO;
using System.Linq;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    public Card[] AllCards {get;set;}        // Master list
    public Card[] InDeck;          // Active deck (current deck)

    private string filePath;
    private const string encryptionKey = "1234567890123456";

    [SerializeField] private bool schuffle = false;
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "deck.dat");
        LoadAllCards();
        LoadDeck();
        ShuffleDeck();
    }

    private void Update() {
        if(schuffle){
            ShuffleDeck();
            schuffle = false;
        }
    }

    void LoadDeck()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Deck file not found.");
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

    public void ShuffleDeck()
    {
        for (int i = 0; i < InDeck.Length; i++)
        {
            int r = Random.Range(i, InDeck.Length);
            (InDeck[i], InDeck[r]) = (InDeck[r], InDeck[i]);
        }
    }

    private void LoadAllCards(){
        AllCards = Resources.LoadAll<Card>("Cards/")
        .OrderBy(_card => _card.cardID)
        .ToArray();
        //load all cards from Resources/Cards/
        if(AllCards.Length == 0){
            Debug.LogError("No cards found in Resources/Cards/");
            return;
        }
    }
}
