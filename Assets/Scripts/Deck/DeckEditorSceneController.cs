using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeckEditorSceneController : MonoBehaviour
{
    [SerializeField] private DeckEditorManager deckEditorManager;
    [SerializeField] private string playSceneName = "play";

    private readonly Color backgroundColor = new Color(0.08f, 0.09f, 0.11f);
    private readonly Color panelColor = new Color(0.14f, 0.16f, 0.19f);
    private readonly Color headerColor = new Color(0.20f, 0.23f, 0.27f);
    private readonly Color buttonColor = new Color(0.24f, 0.30f, 0.36f);
    private readonly Color accentColor = new Color(0.85f, 0.21f, 0.32f);
    private readonly Color mutedTextColor = new Color(0.72f, 0.76f, 0.80f);

    [Header("Scene UI References")]
    [SerializeField] private Canvas deckEditorCanvas;
    [SerializeField] private Font uiFont;
    [SerializeField] private InputField deckNameInput;
    [SerializeField] private InputField encodedDeckInput;
    [SerializeField] private Text deckCountText;
    [SerializeField] private Text statusText;
    [SerializeField] private RectTransform savedDeckList;
    [SerializeField] private RectTransform cardLibraryList;
    [SerializeField] private RectTransform currentDeckList;

    [Header("Scene UI Buttons")]
    [SerializeField] private Button newDeckButton;
    [SerializeField] private Button saveDeckButton;
    [SerializeField] private Button clearDeckButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button refreshSavedDecksButton;
    [SerializeField] private Button reloadCardsButton;
    [SerializeField] private Button sortDeckButton;
    [SerializeField] private Button exportButton;
    [SerializeField] private Button importButton;

    private Card[] allCards = new Card[0];
    private Dictionary<int, Card> cardsById = new Dictionary<int, Card>();

    private void Awake()
    {
        ResolveManagers();
        EnsureEventSystem();
        LoadCardLibrary();
        ResolveFont();
        BindInterface();
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(DeckStorage.GetActiveDeckId()))
        {
            deckEditorManager.LoadActiveDeck();
        }

        RefreshAll();
    }

    private void ResolveManagers()
    {
        if (deckEditorManager == null)
        {
            deckEditorManager = Object.FindFirstObjectByType<DeckEditorManager>();
        }

        if (deckEditorManager == null)
        {
            GameObject managerObject = new GameObject("DeckEditorManager");
            deckEditorManager = managerObject.AddComponent<DeckEditorManager>();
        }

        deckEditorManager.OnDeckChanged.AddListener(_ => RefreshCurrentDeck());
        deckEditorManager.OnDeckSaved.AddListener(_ => RefreshSavedDecks());
        deckEditorManager.OnDeckLoaded.AddListener(_ => SetStatus("Deck loaded."));
        deckEditorManager.OnDeckError.AddListener(SetStatus);
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private void LoadCardLibrary()
    {
        allCards = Resources.LoadAll<Card>("Cards/")
            .OrderBy(card => card.cardID)
            .ThenBy(card => card.DisplayName)
            .ToArray();

        cardsById = allCards
            .GroupBy(card => card.cardID)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private void ResolveFont()
    {
        if (uiFont != null) return;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

    }

    private void BindInterface()
    {
        if (deckEditorCanvas == null)
        {
            Debug.LogError("Deck Editor UI references are missing. Open Tools > Deck Editor > Rebuild Editable UI.", this);
            enabled = false;
            return;
        }

        deckNameInput.onEndEdit.AddListener(deckEditorManager.SetDeckName);
        newDeckButton.onClick.AddListener(CreateNewDeck);
        saveDeckButton.onClick.AddListener(SaveDeck);
        clearDeckButton.onClick.AddListener(ClearDeck);
        playButton.onClick.AddListener(StartFreshPlayfield);
        refreshSavedDecksButton.onClick.AddListener(RefreshSavedDecks);
        reloadCardsButton.onClick.AddListener(ReloadCards);
        sortDeckButton.onClick.AddListener(RefreshCurrentDeck);
        exportButton.onClick.AddListener(ExportDeck);
        importButton.onClick.AddListener(ImportDeck);
    }

    private void CreateNewDeck() { deckEditorManager.CreateNewDeck("New Deck"); SetStatus("New deck created."); RefreshAll(); }
    private void SaveDeck() { deckEditorManager.SaveCurrentDeck(); SetStatus("Deck saved and selected for play."); RefreshAll(); }
    private void ClearDeck() { deckEditorManager.ClearDeck(); SetStatus("Deck cleared."); }
    private void ReloadCards() { LoadCardLibrary(); RefreshCardLibrary(); }
    private void ExportDeck() { encodedDeckInput.text = deckEditorManager.ExportCurrentDeck(); SetStatus("Encoded deck data exported."); }
    private void ImportDeck() { if (deckEditorManager.ImportDeckFromEncodedText(encodedDeckInput.text)) { SetStatus("Encoded deck imported. Save it to keep it locally."); RefreshAll(); } }

#if UNITY_EDITOR
    public void RebuildEditableInterface()
    {
        ResolveFont();
        if (deckEditorCanvas != null) DestroyImmediate(deckEditorCanvas.gameObject);

        GameObject canvasObject = new GameObject("DeckEditorCanvas");
        UnityEditor.Undo.RegisterCreatedObjectUndo(canvasObject, "Build Deck Editor UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        deckEditorCanvas = canvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Image background = canvasObject.AddComponent<Image>();
        background.color = backgroundColor;

        RectTransform root = CreateRect("Root", canvasRect);
        Stretch(root);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(24, 24, 20, 20);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        BuildHeader(root);
        BuildBody(root);
        BuildFooter(root);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void BuildHeader(RectTransform parent)
    {
        RectTransform header = CreatePanel("Header", parent, headerColor);
        AddLayoutElement(header, -1, 84, 0, 0);
        HorizontalLayoutGroup layout = AddHorizontalLayout(header, 12, 18, 18, 14, 14);
        layout.childForceExpandWidth = false;

        Text title = CreateText("Title", header, "Deck Editor", 34, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(title.rectTransform, 240, -1, 0, 1);

        deckNameInput = CreateInputField("DeckNameInput", header, "Deck name");
        AddLayoutElement(deckNameInput.GetComponent<RectTransform>(), 380, 48, 1, 0);
        deckNameInput.onEndEdit.AddListener(deckEditorManager.SetDeckName);

        newDeckButton = CreateButton(header, "New", null);
        saveDeckButton = CreateButton(header, "Save", null, accentColor);
        clearDeckButton = CreateButton(header, "Clear", null);
        playButton = CreateButton(header, "Play", null);
    }

    private void BuildBody(RectTransform parent)
    {
        RectTransform body = CreateRect("Body", parent);
        AddLayoutElement(body, -1, -1, 1, 1);
        HorizontalLayoutGroup layout = AddHorizontalLayout(body, 16, 0, 0, 0, 0);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        savedDeckList = BuildColumn(body, "Saved Decks", "Refresh", out refreshSavedDecksButton, 0.8f);
        cardLibraryList = BuildColumn(body, "Card Library", "Reload", out reloadCardsButton, 1.2f);
        currentDeckList = BuildColumn(body, "Current Deck", "Sort", out sortDeckButton, 1.0f);
    }

    private void BuildFooter(RectTransform parent)
    {
        RectTransform footer = CreatePanel("ImportExport", parent, panelColor);
        AddLayoutElement(footer, -1, 180, 0, 0);
        VerticalLayoutGroup layout = AddVerticalLayout(footer, 10, 18, 18, 14, 14);
        layout.childForceExpandHeight = false;

        Text heading = CreateText("ImportExportTitle", footer, "Encoded Deck Data", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(heading.rectTransform, -1, 26, 0, 0);

        RectTransform row = CreateRect("ImportExportRow", footer);
        AddLayoutElement(row, -1, 92, 1, 0);
        HorizontalLayoutGroup rowLayout = AddHorizontalLayout(row, 12, 0, 0, 0, 0);
        rowLayout.childForceExpandWidth = false;

        encodedDeckInput = CreateInputField("EncodedDeckInput", row, "Exported/imported encoded deck text");
        encodedDeckInput.lineType = InputField.LineType.MultiLineNewline;
        AddLayoutElement(encodedDeckInput.GetComponent<RectTransform>(), -1, -1, 1, 1);

        RectTransform buttonStack = CreateRect("ImportExportButtons", row);
        AddLayoutElement(buttonStack, 160, -1, 0, 1);
        AddVerticalLayout(buttonStack, 8, 0, 0, 0, 0);

        exportButton = CreateButton(buttonStack, "Export", null);
        importButton = CreateButton(buttonStack, "Import", null, accentColor);

        RectTransform statusRow = CreateRect("StatusRow", footer);
        AddLayoutElement(statusRow, -1, 28, 0, 0);
        HorizontalLayoutGroup statusLayout = AddHorizontalLayout(statusRow, 16, 0, 0, 0, 0);
        statusLayout.childForceExpandWidth = false;

        deckCountText = CreateText("DeckCount", statusRow, "", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(deckCountText.rectTransform, 220, -1, 0, 0);

        statusText = CreateText("Status", statusRow, "", 18, FontStyle.Normal, TextAnchor.MiddleLeft, mutedTextColor);
        AddLayoutElement(statusText.rectTransform, -1, -1, 1, 0);
    }

    private RectTransform BuildColumn(RectTransform parent, string title, string actionLabel, out Button actionButton, float flexibleWidth)
    {
        RectTransform column = CreatePanel(title.Replace(" ", "") + "Column", parent, panelColor);
        AddLayoutElement(column, 320, -1, flexibleWidth, 1);
        VerticalLayoutGroup layout = AddVerticalLayout(column, 10, 14, 14, 14, 14);
        layout.childForceExpandHeight = false;

        RectTransform headingRow = CreateRect("Heading", column);
        AddLayoutElement(headingRow, -1, 44, 0, 0);
        HorizontalLayoutGroup headingLayout = AddHorizontalLayout(headingRow, 8, 0, 0, 0, 0);
        headingLayout.childForceExpandWidth = false;

        Text heading = CreateText("Title", headingRow, title, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(heading.rectTransform, -1, -1, 1, 0);
        actionButton = CreateButton(headingRow, actionLabel, null);

        return CreateScrollContent(title.Replace(" ", "") + "List", column);
    }

    private RectTransform CreateScrollContent(string name, RectTransform parent)
    {
        RectTransform viewport = CreateRect(name + "Viewport", parent);
        AddLayoutElement(viewport, -1, -1, 1, 1);

        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0.09f, 0.10f, 0.12f, 0.92f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 30f;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(8f, 0f);
        content.offsetMax = new Vector2(-8f, 0f);

        VerticalLayoutGroup contentLayout = AddVerticalLayout(content, 8, 8, 8, 8, 8);
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        return content;
    }

    private void RefreshAll()
    {
        RefreshSavedDecks();
        RefreshCardLibrary();
        RefreshCurrentDeck();
    }

    private void StartFreshPlayfield()
    {
        if (!deckEditorManager.SaveCurrentDeck())
        {
            SetStatus("Could not save deck before starting play.");
            return;
        }

        PrepareRuntimeDeckManager();
        SceneManager.LoadScene(playSceneName, LoadSceneMode.Single);
    }

    private void PrepareRuntimeDeckManager()
    {
        DeckManager runtimeDeckManager = DeckManager.instance;
        if (runtimeDeckManager == null)
        {
            runtimeDeckManager = Object.FindFirstObjectByType<DeckManager>();
        }

        if (runtimeDeckManager == null)
        {
            GameObject deckManagerObject = new GameObject("DeckManager");
            runtimeDeckManager = deckManagerObject.AddComponent<DeckManager>();
        }

        runtimeDeckManager.UseDeck(deckEditorManager.CurrentDeck);
    }

    private void RefreshSavedDecks()
    {
        if (savedDeckList == null) return;

        ClearChildren(savedDeckList);
        DeckData[] savedDecks = deckEditorManager.GetSavedDecks();

        if (savedDecks.Length == 0)
        {
            AddEmptyMessage(savedDeckList, "No saved decks yet.");
            return;
        }

        string activeDeckId = DeckStorage.GetActiveDeckId();
        foreach (DeckData deck in savedDecks)
        {
            CreateSavedDeckRow(savedDeckList, deck, deck.deckId == activeDeckId);
        }
    }

    private void CreateSavedDeckRow(RectTransform parent, DeckData deck, bool isActiveDeck)
    {
        DeckData capturedDeck = deck;
        RectTransform row = CreatePanel("SavedDeck_" + capturedDeck.deckId, parent, headerColor);
        AddLayoutElement(row, -1, 78, 0, 0);
        HorizontalLayoutGroup layout = AddHorizontalLayout(row, 8, 10, 10, 8, 8);
        layout.childForceExpandWidth = false;

        string activeMarker = isActiveDeck ? " *" : "";
        Text info = CreateText(
            "Info",
            row,
            $"{capturedDeck.deckName}{activeMarker}\n{capturedDeck.cardIds.Length} cards",
            17,
            FontStyle.Normal,
            TextAnchor.MiddleLeft);
        AddLayoutElement(info.rectTransform, -1, -1, 1, 0);

        CreateButton(row, "Load", () =>
        {
            deckEditorManager.LoadDeck(capturedDeck.deckId);
            SetStatus($"Loaded {capturedDeck.deckName}.");
            RefreshAll();
        }, buttonColor, 76, 48);

        CreateButton(row, "Delete", () =>
        {
            if (deckEditorManager.DeleteDeck(capturedDeck.deckId))
            {
                SetStatus($"Deleted {capturedDeck.deckName}.");
                RefreshAll();
            }
        }, accentColor, 92, 48);
    }

    private void RefreshCardLibrary()
    {
        if (cardLibraryList == null) return;

        ClearChildren(cardLibraryList);
        if (allCards.Length == 0)
        {
            AddEmptyMessage(cardLibraryList, "No cards found in Resources/Cards.");
            return;
        }

        foreach (Card card in allCards)
        {
            Card capturedCard = card;
            CreateCardLibraryRow(cardLibraryList, capturedCard);
        }
    }

    private void RefreshCurrentDeck()
    {
        if (currentDeckList == null || deckEditorManager.CurrentDeck == null) return;

        if (deckNameInput != null && !deckNameInput.isFocused)
        {
            deckNameInput.text = deckEditorManager.CurrentDeck.deckName;
        }

        if (deckCountText != null)
        {
            deckCountText.text = $"{deckEditorManager.CurrentDeckCount} cards";
        }

        ClearChildren(currentDeckList);
        int[] cardIds = deckEditorManager.GetCurrentCardIds();
        if (cardIds.Length == 0)
        {
            AddEmptyMessage(currentDeckList, "Add cards from the library.");
            return;
        }

        foreach (var group in cardIds.GroupBy(cardId => cardId).OrderBy(group => group.Key))
        {
            int cardId = group.Key;
            int count = group.Count();
            cardsById.TryGetValue(cardId, out Card card);
            CreateDeckRow(currentDeckList, cardId, count, card);
        }
    }

    private void CreateCardLibraryRow(RectTransform parent, Card card)
    {
        RectTransform row = CreatePanel("Card_" + card.cardID, parent, headerColor);
        AddLayoutElement(row, -1, 94, 0, 0);
        HorizontalLayoutGroup layout = AddHorizontalLayout(row, 10, 10, 10, 8, 8);
        layout.childForceExpandWidth = false;

        Image thumbnail = CreateImage("Thumbnail", row, card.FrontImage);
        AddLayoutElement(thumbnail.rectTransform, 58, 78, 0, 0);

        Text info = CreateText("Info", row, FormatCardLabel(card), 17, FontStyle.Normal, TextAnchor.MiddleLeft);
        AddLayoutElement(info.rectTransform, -1, -1, 1, 0);

        CreateButton(row, "+", () =>
        {
            if (deckEditorManager.AddCard(card))
            {
                SetStatus($"Added {card.DisplayName}.");
            }
        }, accentColor, 52, 52);
    }

    private void CreateDeckRow(RectTransform parent, int cardId, int count, Card card)
    {
        RectTransform row = CreatePanel("DeckCard_" + cardId, parent, headerColor);
        AddLayoutElement(row, -1, 72, 0, 0);
        HorizontalLayoutGroup layout = AddHorizontalLayout(row, 8, 10, 10, 8, 8);
        layout.childForceExpandWidth = false;

        string cardName = card != null ? card.DisplayName : "Unknown Card";
        Text info = CreateText("Info", row, $"{cardName}\nID {cardId} x{count}", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
        AddLayoutElement(info.rectTransform, -1, -1, 1, 0);

        CreateButton(row, "-", () => deckEditorManager.RemoveCardById(cardId), buttonColor, 48, 48);
        CreateButton(row, "+", () => deckEditorManager.AddCardById(cardId), accentColor, 48, 48);
    }

    private string FormatCardLabel(Card card)
    {
        return $"{card.DisplayName}\nID {card.cardID} | {card.Type}";
    }

    private Button CreateListButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(parent, label, action, headerColor, -1, 68);
        Text text = button.GetComponentInChildren<Text>();
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 17;
        return button;
    }

    private Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, Color? color = null, float width = 108f, float height = 44f)
    {
        RectTransform rect = CreateRect(label + "Button", parent);
        AddLayoutElement(rect, width, height, width < 0 ? 1 : 0, 0);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color ?? buttonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (action != null) button.onClick.AddListener(action);

        Text text = CreateText("Label", rect, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 8, 8, 4, 4);
        return button;
    }

    private InputField CreateInputField(string name, RectTransform parent, string placeholder)
    {
        RectTransform rect = CreatePanel(name, parent, new Color(0.08f, 0.10f, 0.12f));

        InputField input = rect.gameObject.AddComponent<InputField>();
        input.targetGraphic = rect.GetComponent<Image>();
        input.selectionColor = new Color(1f, 1f, 1f, 0.25f);

        Text text = CreateText("Text", rect, "", 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        Stretch(text.rectTransform, 12, 12, 8, 8);
        input.textComponent = text;

        Text placeholderText = CreateText("Placeholder", rect, placeholder, 18, FontStyle.Italic, TextAnchor.MiddleLeft, mutedTextColor);
        Stretch(placeholderText.rectTransform, 12, 12, 8, 8);
        input.placeholder = placeholderText;

        return input;
    }

    private Image CreateImage(string name, RectTransform parent, Sprite sprite)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite == null ? new Color(0.05f, 0.06f, 0.07f) : Color.white;
        image.preserveAspect = true;
        return image;
    }

    private Text CreateText(string name, RectTransform parent, string value, int size, FontStyle style, TextAnchor alignment, Color? color = null)
    {
        RectTransform rect = CreateRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color ?? Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private HorizontalLayoutGroup AddHorizontalLayout(RectTransform rect, float spacing, int left, int right, int top, int bottom)
    {
        HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(left, right, top, bottom);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return layout;
    }

    private VerticalLayoutGroup AddVerticalLayout(RectTransform rect, float spacing, int left, int right, int top, int bottom)
    {
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(left, right, top, bottom);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private void AddLayoutElement(RectTransform rect, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
    {
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        if (preferredWidth >= 0) element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0) element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
    }

    private void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void AddEmptyMessage(RectTransform parent, string message)
    {
        Text text = CreateText("Empty", parent, message, 18, FontStyle.Italic, TextAnchor.MiddleCenter, mutedTextColor);
        AddLayoutElement(text.rectTransform, -1, 80, 1, 0);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
