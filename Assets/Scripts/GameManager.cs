using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState
    {
        NotStarted,
        Playing,
        Finished
    }

    public enum GamePhase
    {
        Setup,
        Draw,
        Main,
        Live,
        End
    }

    private enum DeckPlacementChoice
    {
        Top,
        Bottom,
        Shuffle
    }

    [System.Serializable]
    public class PhaseChangedEvent : UnityEvent<GamePhase> { }

    [System.Serializable]
    public class TurnChangedEvent : UnityEvent<int> { }

    [System.Serializable]
    public class CardDrawnEvent : UnityEvent<Card> { }

    [Header("Scene References")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;
    [SerializeField] private DressingRoomManager dressingRoomManager;

    [Header("Game Start")]
    [SerializeField] private bool autoStartGame = true;
    [SerializeField] private bool createMissingManagers = true;
    [SerializeField] private bool shuffleOnStart = true;
    [SerializeField, Min(0)] private int openingHandSize = 5;
    [SerializeField] private GamePhase firstPhaseAfterSetup = GamePhase.Draw;

    [Header("Draw Phase")]
    [SerializeField] private bool drawAtStartOfDrawPhase = true;
    [SerializeField] private bool skipFirstTurnDraw = true;

    [Header("Deck Interaction")]
    [SerializeField, FormerlySerializedAs("drawRandomCardByDraggingDeck")] private bool drawCardByDraggingDeck = true;
    [SerializeField, Min(1f)] private float deckDragThresholdPixels = 40f;
    [SerializeField] private Card.PositionStatus deckFieldPosition = Card.PositionStatus.InDeck;

    [Header("Events")]
    public UnityEvent OnGameStarted = new UnityEvent();
    public UnityEvent OnGameFinished = new UnityEvent();
    public PhaseChangedEvent OnPhaseChanged = new PhaseChangedEvent();
    public TurnChangedEvent OnTurnChanged = new TurnChangedEvent();
    public CardDrawnEvent OnCardDrawn = new CardDrawnEvent();

    public GameState State { get; private set; } = GameState.NotStarted;
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Setup;
    public int TurnNumber { get; private set; } = 0;
    public int RemainingDeckCards => deckManager != null ? deckManager.RemainingCards : 0;

    private Camera mainCamera;
    private FieldRaycastHandler deckField;
    private bool deckPressStartedOverDeck;
    private bool deckDrawTriggered;
    private Vector2 deckPressStartPosition;
    private GameObject deckPlacementPopupRoot;
    private Card pendingDeckPlacementCard;
    private Font uiFont;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResolveManagers();
    }

    private void Start()
    {
        if (autoStartGame)
        {
            StartGame();
        }
    }

    private void Update()
    {
        HandleDeckDragDraw();
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;

        ResolveManagers();
        if (deckManager == null)
        {
            Debug.LogError("Cannot start game because no DeckManager exists.");
            return;
        }

        State = GameState.Playing;
        TurnNumber = 1;
        CurrentPhase = GamePhase.Setup;

        if (shuffleOnStart)
        {
            deckManager.ShuffleDeck();
        }

        OnGameStarted.Invoke();
        OnTurnChanged.Invoke(TurnNumber);

        DrawCards(openingHandSize);
        SetPhase(firstPhaseAfterSetup, true);
    }

    public void FinishGame()
    {
        if (State == GameState.Finished) return;

        State = GameState.Finished;
        OnGameFinished.Invoke();
    }

    public void NextPhase()
    {
        if (State == GameState.NotStarted)
        {
            StartGame();
            return;
        }

        if (State != GameState.Playing) return;

        switch (CurrentPhase)
        {
            case GamePhase.Setup:
                SetPhase(GamePhase.Draw);
                break;
            case GamePhase.Draw:
                SetPhase(GamePhase.Main);
                break;
            case GamePhase.Main:
                SetPhase(GamePhase.Live);
                break;
            case GamePhase.Live:
                SetPhase(GamePhase.End);
                break;
            case GamePhase.End:
                StartNextTurn();
                break;
        }
    }

    public void StartNextTurn()
    {
        if (State != GameState.Playing) return;

        TurnNumber++;
        OnTurnChanged.Invoke(TurnNumber);
        SetPhase(GamePhase.Draw, true);
    }

    public void SetDrawPhase()
    {
        SetPhase(GamePhase.Draw);
    }

    public void SetMainPhase()
    {
        SetPhase(GamePhase.Main);
    }

    public void SetLivePhase()
    {
        SetPhase(GamePhase.Live);
    }

    public void SetEndPhase()
    {
        SetPhase(GamePhase.End);
    }

    public void DrawCard()
    {
        ResolveManagers();
        if (deckManager == null) return;

        Card drawnCard = deckManager.DrawCard(GetDeckDrawPosition());
        if (drawnCard != null)
        {
            OnCardDrawn.Invoke(drawnCard);
        }
    }

    public void DrawRandomCard()
    {
        ResolveManagers();
        if (deckManager == null) return;

        Card drawnCard = deckManager.DrawRandomCard(GetDeckDrawPosition());
        if (drawnCard != null)
        {
            OnCardDrawn.Invoke(drawnCard);
        }
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            DrawCard();
        }
    }

    public void ShuffleDeck()
    {
        ResolveManagers();
        deckManager?.ShuffleDeck();
    }

    public bool TryShowDeckPlacementPrompt(Card card)
    {
        ResolveManagers();
        ResolveDeckField();

        if (card == null || deckManager == null || deckField == null)
        {
            return false;
        }

        if (deckPlacementPopupRoot != null)
        {
            card.AddToHand();
            return true;
        }

        pendingDeckPlacementCard = card;
        pendingDeckPlacementCard.SetPositionStatus(Card.PositionStatus.InDeck);
        ShowDeckPlacementPopup();
        return true;
    }

    private void SetPhase(GamePhase phase, bool force = false)
    {
        if (State != GameState.Playing) return;
        if (!force && CurrentPhase == phase) return;

        CurrentPhase = phase;
        OnPhaseChanged.Invoke(CurrentPhase);
        HandlePhaseStart();
    }

    private void HandlePhaseStart()
    {
        if (CurrentPhase != GamePhase.Draw) return;
        if (!drawAtStartOfDrawPhase) return;
        if (skipFirstTurnDraw && TurnNumber == 1) return;

        DrawCard();
    }

    private void ResolveManagers()
    {
        if (deckManager == null)
        {
            deckManager = Object.FindFirstObjectByType<DeckManager>();
        }

        if (handManager == null)
        {
            handManager = Object.FindFirstObjectByType<HandManager>();
        }

        if (dressingRoomManager == null)
        {
            dressingRoomManager = Object.FindFirstObjectByType<DressingRoomManager>();
        }

        if (!createMissingManagers) return;

        if (deckManager == null)
        {
            GameObject deckManagerObject = new GameObject("DeckManager");
            deckManager = deckManagerObject.AddComponent<DeckManager>();
        }

        if (handManager == null)
        {
            GameObject handManagerObject = new GameObject("HandManager");
            handManager = handManagerObject.AddComponent<HandManager>();
        }

        if (dressingRoomManager == null)
        {
            GameObject dressingRoomManagerObject = new GameObject("DressingRoomManager");
            dressingRoomManager = dressingRoomManagerObject.AddComponent<DressingRoomManager>();
        }
    }

    private void HandleDeckDragDraw()
    {
        if (deckPlacementPopupRoot != null) return;
        if (!drawCardByDraggingDeck) return;
        if (Mouse.current != null)
        {
            HandleDeckPointer(
                Mouse.current.leftButton.isPressed,
                Mouse.current.leftButton.wasPressedThisFrame,
                Mouse.current.leftButton.wasReleasedThisFrame,
                Mouse.current.position.ReadValue());
        }

        if (Touchscreen.current == null) return;

        TouchControl touch = Touchscreen.current.primaryTouch;
        HandleDeckPointer(
            touch.press.isPressed,
            touch.press.wasPressedThisFrame,
            touch.press.wasReleasedThisFrame,
            touch.position.ReadValue());
    }

    private void HandleDeckPointer(bool isPressed, bool wasPressedThisFrame, bool wasReleasedThisFrame, Vector2 screenPosition)
    {
        if (wasPressedThisFrame)
        {
            deckPressStartedOverDeck = IsPointerOverDeck(screenPosition);
            deckDrawTriggered = false;
            deckPressStartPosition = screenPosition;
        }

        if (!isPressed)
        {
            if (wasReleasedThisFrame)
            {
                deckPressStartedOverDeck = false;
                deckDrawTriggered = false;
            }

            return;
        }

        if (!deckPressStartedOverDeck || deckDrawTriggered) return;
        if (Vector2.Distance(deckPressStartPosition, screenPosition) < deckDragThresholdPixels) return;

        DrawCard();
        deckDrawTriggered = true;
    }

    private bool IsPointerOverDeck(Vector2 screenPosition)
    {
        ResolveDeckField();
        if (deckField == null) return false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return false;

        return hit.transform == deckField.transform || hit.transform.IsChildOf(deckField.transform);
    }

    private void ResolveDeckField()
    {
        if (deckField != null) return;

        FieldRaycastHandler[] fields = Object.FindObjectsByType<FieldRaycastHandler>(FindObjectsSortMode.None);
        foreach (FieldRaycastHandler field in fields)
        {
            if (field.FieldPosition != deckFieldPosition) continue;

            deckField = field;
            return;
        }
    }

    private Vector3 GetDeckDrawPosition()
    {
        ResolveDeckField();
        return deckField != null ? deckField.transform.position : deckManager.transform.position;
    }

    private void ShowDeckPlacementPopup()
    {
        EnsureEventSystem();

        deckPlacementPopupRoot = new GameObject("DeckPlacementPopup", typeof(RectTransform));
        Canvas canvas = deckPlacementPopupRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        deckPlacementPopupRoot.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = deckPlacementPopupRoot.GetComponent<RectTransform>();

        RectTransform backdrop = CreateUIRect("Backdrop", canvasRect);
        Stretch(backdrop);
        Image backdropImage = backdrop.gameObject.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panel = CreateUIRect("Panel", backdrop);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(460f, 300f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.14f, 0.17f, 0.98f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreatePopupText("Title", panel, "카드를 덱에 어떻게 둘까요?", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddLayoutElement(title.rectTransform, -1, 44, 1, 0);

        CreatePopupButton(panel, "덱 위에 두기", () => ResolveDeckPlacement(DeckPlacementChoice.Top));
        CreatePopupButton(panel, "덱 아래에 두기", () => ResolveDeckPlacement(DeckPlacementChoice.Bottom));
        CreatePopupButton(panel, "덱에 넣고 섞기", () => ResolveDeckPlacement(DeckPlacementChoice.Shuffle));
    }

    private void ResolveDeckPlacement(DeckPlacementChoice choice)
    {
        Card card = pendingDeckPlacementCard;
        pendingDeckPlacementCard = null;

        bool handled = false;
        if (card != null && deckManager != null)
        {
            switch (choice)
            {
                case DeckPlacementChoice.Top:
                    handled = deckManager.PutCardOnTop(card);
                    break;
                case DeckPlacementChoice.Bottom:
                    handled = deckManager.PutCardOnBottom(card);
                    break;
                case DeckPlacementChoice.Shuffle:
                    handled = deckManager.PutCardOnTopAndShuffle(card);
                    break;
            }
        }

        CloseDeckPlacementPopup();
        if (!handled && card != null)
        {
            card.AddToHand();
        }
    }

    private void CloseDeckPlacementPopup()
    {
        if (deckPlacementPopupRoot == null) return;

        Destroy(deckPlacementPopupRoot);
        deckPlacementPopupRoot = null;
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private Font GetUIFont()
    {
        if (uiFont != null) return uiFont;

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return uiFont;
    }

    private RectTransform CreateUIRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Text CreatePopupText(string name, RectTransform parent, string value, int size, FontStyle style, TextAnchor alignment)
    {
        RectTransform rect = CreateUIRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = GetUIFont();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private void CreatePopupButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateUIRect(label + "Button", parent);
        AddLayoutElement(rect, -1, 56, 1, 0);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.82f, 0.18f, 0.28f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreatePopupText("Label", rect, label, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 12, 12, 6, 6);
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
}
