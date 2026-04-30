using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class DeckEditorManager : MonoBehaviour
{
    [System.Serializable]
    public class DeckChangedEvent : UnityEvent<DeckData> { }

    [System.Serializable]
    public class DeckErrorEvent : UnityEvent<string> { }

    [Header("New Deck Defaults")]
    [SerializeField] private string defaultDeckName = "New Deck";

    [Header("Rules")]
    [SerializeField] private bool enforceMaxCopiesPerCard = false;
    [SerializeField, Min(1)] private int maxCopiesPerCard = 4;
    [SerializeField, Min(0)] private int maxDeckSize = 0;

    [Header("Save Behavior")]
    [SerializeField] private bool setSavedDeckAsActive = true;
    [SerializeField] private bool applySavedDeckToRuntimeDeckManager = true;

    [Header("Events")]
    public DeckChangedEvent OnDeckChanged = new DeckChangedEvent();
    public DeckChangedEvent OnDeckLoaded = new DeckChangedEvent();
    public DeckChangedEvent OnDeckSaved = new DeckChangedEvent();
    public DeckErrorEvent OnDeckError = new DeckErrorEvent();

    public DeckData CurrentDeck { get; private set; }
    public string LastEncodedDeckData { get; private set; } = string.Empty;
    public int CurrentDeckCount => editingCardIds.Count;

    private readonly List<int> editingCardIds = new List<int>();

    private void Awake()
    {
        CreateNewDeck(defaultDeckName);
    }

    public void CreateNewDeck()
    {
        CreateNewDeck(defaultDeckName);
    }

    public void CreateNewDeck(string deckName)
    {
        CurrentDeck = DeckStorage.CreateDeck(deckName);
        editingCardIds.Clear();
        LastEncodedDeckData = string.Empty;
        NotifyDeckChanged();
    }

    public bool LoadDeck(string deckId)
    {
        if (!DeckStorage.TryLoadDeck(deckId, out DeckData deckData, out string error))
        {
            ReportError($"Could not load deck: {error}");
            return false;
        }

        BeginEditing(deckData);
        OnDeckLoaded.Invoke(CurrentDeck);
        return true;
    }

    public bool LoadActiveDeck()
    {
        if (!DeckStorage.TryLoadActiveDeck(out DeckData deckData, out string error))
        {
            ReportError($"Could not load active deck: {error}");
            return false;
        }

        BeginEditing(deckData);
        OnDeckLoaded.Invoke(CurrentDeck);
        return true;
    }

    public DeckData[] GetSavedDecks()
    {
        return DeckStorage.LoadAllDecks();
    }

    public void SetDeckName(string deckName)
    {
        EnsureCurrentDeck();
        CurrentDeck.deckName = string.IsNullOrWhiteSpace(deckName) ? defaultDeckName : deckName.Trim();
        NotifyDeckChanged();
    }

    public bool AddCard(Card card)
    {
        if (card == null)
        {
            ReportError("Cannot add a missing card.");
            return false;
        }

        return AddCardById(card.cardID);
    }

    public bool AddCardById(int cardId)
    {
        EnsureCurrentDeck();

        if (maxDeckSize > 0 && editingCardIds.Count >= maxDeckSize)
        {
            ReportError($"Deck already has the maximum of {maxDeckSize} cards.");
            return false;
        }

        if (enforceMaxCopiesPerCard && editingCardIds.Count(id => id == cardId) >= maxCopiesPerCard)
        {
            ReportError($"Deck already has the maximum of {maxCopiesPerCard} copies for card id {cardId}.");
            return false;
        }

        editingCardIds.Add(cardId);
        NotifyDeckChanged();
        return true;
    }

    public bool RemoveCard(Card card)
    {
        if (card == null)
        {
            ReportError("Cannot remove a missing card.");
            return false;
        }

        return RemoveCardById(card.cardID);
    }

    public bool RemoveCardById(int cardId)
    {
        EnsureCurrentDeck();

        if (!editingCardIds.Remove(cardId))
        {
            ReportError($"Deck does not contain card id {cardId}.");
            return false;
        }

        NotifyDeckChanged();
        return true;
    }

    public bool RemoveCardAt(int index)
    {
        EnsureCurrentDeck();

        if (index < 0 || index >= editingCardIds.Count)
        {
            ReportError($"Card index is out of range: {index}.");
            return false;
        }

        editingCardIds.RemoveAt(index);
        NotifyDeckChanged();
        return true;
    }

    public void ClearDeck()
    {
        EnsureCurrentDeck();
        editingCardIds.Clear();
        NotifyDeckChanged();
    }

    public bool SaveCurrentDeck()
    {
        EnsureCurrentDeck();
        CurrentDeck.cardIds = editingCardIds.ToArray();

        string path = DeckStorage.SaveDeck(CurrentDeck);
        LastEncodedDeckData = DeckStorage.EncodeDeck(CurrentDeck);

        if (setSavedDeckAsActive)
        {
            DeckStorage.SetActiveDeck(CurrentDeck.deckId);
        }

        if (applySavedDeckToRuntimeDeckManager && DeckManager.instance != null)
        {
            DeckManager.instance.UseDeck(CurrentDeck);
        }

        Debug.Log($"Saved deck '{CurrentDeck.deckName}' to {path}");
        OnDeckSaved.Invoke(CurrentDeck);
        NotifyDeckChanged();
        return true;
    }

    public bool DeleteCurrentDeck()
    {
        if (CurrentDeck == null || string.IsNullOrWhiteSpace(CurrentDeck.deckId))
        {
            ReportError("No current deck is selected.");
            return false;
        }

        return DeleteDeck(CurrentDeck.deckId);
    }

    public bool DeleteDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            ReportError("No deck was selected for deletion.");
            return false;
        }

        bool deletingCurrentDeck = CurrentDeck != null && CurrentDeck.deckId == deckId;
        bool deleted = DeckStorage.DeleteDeck(deckId);
        if (deleted)
        {
            if (deletingCurrentDeck)
            {
                CreateNewDeck(defaultDeckName);
            }
        }
        else
        {
            ReportError("Could not delete deck.");
        }

        return deleted;
    }

    public string ExportCurrentDeck()
    {
        EnsureCurrentDeck();
        CurrentDeck.cardIds = editingCardIds.ToArray();
        CurrentDeck.Touch();
        LastEncodedDeckData = DeckStorage.EncodeDeck(CurrentDeck);
        return LastEncodedDeckData;
    }

    public bool ImportDeckFromEncodedText(string encodedDeckData)
    {
        if (!DeckStorage.TryImportDeck(encodedDeckData, out DeckData deckData, out string error, false))
        {
            ReportError($"Could not import deck: {error}");
            return false;
        }

        BeginEditing(deckData);
        return true;
    }

    public int[] GetCurrentCardIds()
    {
        return editingCardIds.ToArray();
    }

    private void BeginEditing(DeckData deckData)
    {
        CurrentDeck = CopyDeck(deckData);
        CurrentDeck.EnsureMetadata();

        editingCardIds.Clear();
        if (CurrentDeck.cardIds != null)
        {
            editingCardIds.AddRange(CurrentDeck.cardIds);
        }

        LastEncodedDeckData = DeckStorage.EncodeDeck(CurrentDeck);
        NotifyDeckChanged();
    }

    private void EnsureCurrentDeck()
    {
        if (CurrentDeck != null) return;

        CurrentDeck = DeckStorage.CreateDeck(defaultDeckName);
        editingCardIds.Clear();
    }

    private void NotifyDeckChanged()
    {
        EnsureCurrentDeck();
        CurrentDeck.cardIds = editingCardIds.ToArray();
        OnDeckChanged.Invoke(CurrentDeck);
    }

    private void ReportError(string error)
    {
        Debug.LogWarning(error);
        OnDeckError.Invoke(error);
    }

    private static DeckData CopyDeck(DeckData deckData)
    {
        string json = JsonUtility.ToJson(deckData);
        return JsonUtility.FromJson<DeckData>(json);
    }
}
