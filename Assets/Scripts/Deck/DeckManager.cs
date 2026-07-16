using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    public Card[] AllCards {get; private set;}        // Master list
    public Card[] InDeck = new Card[0];          // Active deck (current deck)
    public DeckData ActiveDeckData { get; private set; }
    public string ActiveDeckName => ActiveDeckData != null ? ActiveDeckData.deckName : string.Empty;
    public int RemainingCards => InDeck == null ? 0 : Mathf.Max(0, InDeck.Length - nextCardIndex);

    private int nextCardIndex = 0;

    [SerializeField] private bool loadSelectedSavedDeck = true;
    [SerializeField] private bool persistBetweenScenes = true;
    [SerializeField] private bool schuffle = false;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadAllCards();
        LoadStartingDeck();
        ShuffleDeck();
    }

    private void Update() {
        if(schuffle){
            ShuffleDeck();
            schuffle = false;
        }
    }

    public bool LoadSavedDeck(string deckId)
    {
        if (!DeckStorage.TryLoadDeck(deckId, out DeckData deckData, out string error))
        {
            Debug.LogWarning($"Could not load saved deck '{deckId}': {error}");
            return false;
        }

        UseDeck(deckData);
        DeckStorage.SetActiveDeck(deckData.deckId);
        return true;
    }

    public void UseDeck(DeckData deckData)
    {
        if(deckData == null)
        {
            ActiveDeckData = null;
            SetDeck(new Card[0]);
            return;
        }

        deckData.EnsureMetadata();
        ActiveDeckData = deckData;
        SetDeck(ResolveCards(deckData.cardIds));
    }

    public DeckData[] GetSavedDecks()
    {
        return DeckStorage.LoadAllDecks();
    }

    private void LoadStartingDeck()
    {
        string activeDeckError = string.Empty;
        if(loadSelectedSavedDeck && DeckStorage.TryLoadActiveDeck(out DeckData activeDeckData, out activeDeckError))
        {
            UseDeck(activeDeckData);
            return;
        }

        if(loadSelectedSavedDeck && TryMigrateLegacyDeck())
        {
            return;
        }

        if(loadSelectedSavedDeck && !string.IsNullOrWhiteSpace(activeDeckError))
        {
            Debug.LogWarning(activeDeckError);
        }

        Debug.LogWarning("No saved deck found. Using one copy of each loaded card.");
        ActiveDeckData = DeckStorage.CreateDeck("Fallback Deck", AllCards.Select(card => card.cardID));
        SetDeck(AllCards);
    }

    private bool TryMigrateLegacyDeck()
    {
        if(!DeckStorage.TryLoadLegacyDeck(out DeckData legacyDeckData, out string error))
        {
            if(error != "Legacy deck.dat was not found.")
            {
                Debug.LogWarning($"Could not load legacy deck.dat: {error}");
            }

            return false;
        }

        DeckStorage.SaveDeck(legacyDeckData);
        DeckStorage.SetActiveDeck(legacyDeckData.deckId);
        UseDeck(legacyDeckData);
        Debug.Log($"Imported legacy deck.dat as '{legacyDeckData.deckName}'.");
        return true;
    }

    private Card[] ResolveCards(int[] cardIds)
    {
        if(cardIds == null || cardIds.Length == 0) return new Card[0];

        var cardsById = AllCards
            .GroupBy(_card => _card.cardID)
            .ToDictionary(group => group.Key, group => group.First());

        Card[] loadedDeck = cardIds
            .Select(cardId => cardsById.TryGetValue(cardId, out Card loadedCard) ? loadedCard : null)
            .Where(loadedCard => loadedCard != null)
            .ToArray();

        if(loadedDeck.Length != cardIds.Length)
        {
            Debug.LogWarning($"Deck referenced {cardIds.Length - loadedDeck.Length} card id(s) that are not in Resources/Cards/.");
        }

        return loadedDeck;
    }

    public void ShuffleDeck()
    {
        if(InDeck == null || InDeck.Length <= 1) return;

        for (int i = nextCardIndex; i < InDeck.Length; i++)
        {
            int r = UnityEngine.Random.Range(i, InDeck.Length);
            (InDeck[i], InDeck[r]) = (InDeck[r], InDeck[i]);
        }
    }

    public Card DrawCard()
    {
        return DrawCard(transform.position);
    }

    public Card DrawCard(Vector3 drawPosition)
    {
        if(InDeck == null || nextCardIndex >= InDeck.Length)
        {
            Debug.LogWarning("Cannot draw a card because the deck is empty.");
            return null;
        }

        Card cardPrefab = InDeck[nextCardIndex++];
        if(cardPrefab == null) return null;

        Card drawnCard = Instantiate(cardPrefab, drawPosition, Quaternion.identity);
        drawnCard.AddToHand();
        return drawnCard;
    }

    public Card PeekTopCard()
    {
        if(InDeck == null || nextCardIndex >= InDeck.Length)
        {
            return null;
        }

        return InDeck[nextCardIndex];
    }

    public Card DrawRandomCard()
    {
        return DrawRandomCard(transform.position);
    }

    public Card DrawRandomCard(Vector3 drawPosition)
    {
        if(InDeck == null || nextCardIndex >= InDeck.Length)
        {
            Debug.LogWarning("Cannot draw a random card because the deck is empty.");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(nextCardIndex, InDeck.Length);
        (InDeck[nextCardIndex], InDeck[randomIndex]) = (InDeck[randomIndex], InDeck[nextCardIndex]);
        return DrawCard(drawPosition);
    }

    public bool PutCardOnTop(Card card)
    {
        return InsertCardInRemainingDeck(card, nextCardIndex);
    }

    public bool PutCardOnBottom(Card card)
    {
        int bottomIndex = InDeck != null ? InDeck.Length : 0;
        return InsertCardInRemainingDeck(card, bottomIndex);
    }

    public bool PutCardOnTopAndShuffle(Card card)
    {
        if (!PutCardOnTop(card)) return false;

        ShuffleDeck();
        return true;
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

    private void SetDeck(Card[] cards){
        InDeck = cards ?? new Card[0];
        nextCardIndex = 0;
    }

    private bool InsertCardInRemainingDeck(Card card, int insertIndex)
    {
        if (card == null) return false;

        Card cardPrefab = FindCardPrefab(card.cardID);
        if (cardPrefab == null)
        {
            Debug.LogWarning($"Cannot put card id {card.cardID} into the deck because no matching prefab exists.");
            return false;
        }

        List<Card> cards = InDeck != null ? InDeck.ToList() : new List<Card>();
        int safeIndex = Mathf.Clamp(insertIndex, nextCardIndex, cards.Count);
        cards.Insert(safeIndex, cardPrefab);
        InDeck = cards.ToArray();

        Destroy(card.gameObject);
        return true;
    }

    private Card FindCardPrefab(int cardId)
    {
        if (AllCards == null || AllCards.Length == 0)
        {
            LoadAllCards();
        }

        return AllCards.FirstOrDefault(card => card.cardID == cardId);
    }
}
