using UnityEngine;

public class DeckFileCreator : MonoBehaviour
{
    public Card[] AllCards;

    [SerializeField] private bool createOnAwake = false;
    [SerializeField] private bool setAsActiveDeck = true;
    [SerializeField] private string deckName = "Example Deck";
    [SerializeField] private int[] cardIds =
    {
        0, 0, 0, 0,
        1, 1, 1, 1,
        2, 2, 2, 2,
        3, 3, 3, 3,
        4, 4, 4, 4,
        5, 5, 5, 5
    };

    private void Awake()
    {
        if (createOnAwake)
        {
            CreateExampleDeck();
        }
    }

    public void CreateExampleDeck()
    {
        DeckData deck = DeckStorage.CreateDeck(deckName, cardIds);
        string path = DeckStorage.SaveDeck(deck);

        if (setAsActiveDeck)
        {
            DeckStorage.SetActiveDeck(deck.deckId);
        }

        Debug.Log($"Saved encoded deck '{deck.deckName}' to:\n{path}");
    }

    public string CreateEncodedExampleDeckData()
    {
        DeckData deck = DeckStorage.CreateDeck(deckName, cardIds);
        return DeckStorage.EncodeDeck(deck);
    }
}
