using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class DressingRoomManager : MonoBehaviour
{
    public static DressingRoomManager instance;

    [Header("Field")]
    [SerializeField] private FieldRaycastHandler dressingRoomField;
    [SerializeField] private Card.PositionStatus dressingRoomPosition = Card.PositionStatus.DressingRoom;
    [SerializeField, Range(0f, 1f)] private float handDropScreenHeightPercent = 0.35f;

    [Header("Viewer")]
    [SerializeField] private string viewerCanvasResourcePath = "UI/DressingRoomViewCanvas";

    private readonly List<CardDefinition> cards = new List<CardDefinition>();
    private Card cardPrefab;
    private SpriteRenderer fieldSpriteRenderer;
    private Sprite defaultFieldSprite;
    private GameObject optionPopupRoot;
    private GameObject viewerRoot;
    private GameObject confirmationRoot;
    private GameObject dragGhost;
    private Canvas activeCanvas;
    private Font uiFont;
    private int pendingMoveToHandIndex = -1;

    private const string OptionCheckLabel = "\ub300\uae30\uc2e4 \ud655\uc778";
    private const string CancelLabel = "\ucde8\uc18c";
    private const string ViewerTitle = "\ub300\uae30\uc2e4";
    private const string EmptyMessage = "\ub300\uae30\uc2e4\uc5d0 \uce74\ub4dc\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.";
    private const string ConfirmMoveMessage = "\uc774 \uce74\ub4dc\ub97c \uc190\ud328\ub85c \uac00\uc838\uc62c\uae4c\uc694?";
    private const string ConfirmLabel = "\ud655\uc778";

    public int CardCount => cards.Count;

    public static DressingRoomManager GetOrCreate()
    {
        if (instance != null) return instance;

        DressingRoomManager foundManager = Object.FindFirstObjectByType<DressingRoomManager>();
        if (foundManager != null) return foundManager;

        GameObject managerObject = new GameObject("DressingRoomManager");
        return managerObject.AddComponent<DressingRoomManager>();
    }

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

        LoadCardLibrary();
        ResolveDressingRoomField();
    }

    private void Update()
    {
        HandleRightClick();
    }

    public bool PlaceCardOnTop(Card card, FieldRaycastHandler targetField = null)
    {
        if (card == null) return false;

        if (targetField != null)
        {
            dressingRoomField = targetField;
            CacheFieldRenderer();
        }
        else
        {
            ResolveDressingRoomField();
        }

        CardDefinition definition = FindCardDefinition(card.cardID);
        if (definition == null)
        {
            Debug.LogWarning($"Cannot place card id {card.cardID} in the dressing room because no matching prefab exists.");
            return false;
        }

        CancelPendingInteraction();
        cards.Insert(0, definition);
        UpdateTopSprite();
        Destroy(card.gameObject);
        return true;
    }

    public void CancelPendingInteraction()
    {
        CloseViewer();
        CloseOptionPopup();
        CloseConfirmation();
        DestroyDragGhost();
    }

    public void BeginViewerCardDrag(DressingRoomViewerCard viewCard, int cardIndex, PointerEventData eventData)
    {
        if (!IsValidCardIndex(cardIndex)) return;

        CloseConfirmation();
        EnsureActiveCanvas();

        dragGhost = new GameObject("DressingRoomDragGhost", typeof(RectTransform));
        dragGhost.transform.SetParent(activeCanvas.transform, false);

        Image image = dragGhost.AddComponent<Image>();
        image.sprite = null;
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rect = dragGhost.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(116f, 160f);
        rect.position = eventData.position;
    }

    public void UpdateViewerCardDrag(PointerEventData eventData)
    {
        if (dragGhost == null) return;

        dragGhost.GetComponent<RectTransform>().position = eventData.position;
    }

    public void EndViewerCardDrag(DressingRoomViewerCard viewCard, int cardIndex, PointerEventData eventData)
    {
        DestroyDragGhost();
        if (!IsValidCardIndex(cardIndex)) return;

        if (IsPointerOverHand(eventData.position))
        {
            ShowMoveToHandConfirmation(cardIndex);
        }
    }

    private void HandleRightClick()
    {
        if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame) return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        if (IsPointerOverDressingRoom(pointerPosition))
        {
            ShowOptionPopup(pointerPosition);
            return;
        }

        if (optionPopupRoot != null)
        {
            CloseOptionPopup();
        }
    }

    private void ShowOptionPopup(Vector2 screenPosition)
    {
        CancelPendingInteraction();
        EnsureEventSystem();

        optionPopupRoot = new GameObject("DressingRoomOptions", typeof(RectTransform));
        Canvas canvas = optionPopupRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        optionPopupRoot.AddComponent<GraphicRaycaster>();

        RectTransform panel = CreatePanel(optionPopupRoot.transform, new Vector2(220f, 132f), new Color(0.12f, 0.14f, 0.17f, 0.98f));
        panel.position = ClampPopupPosition(screenPosition, panel.sizeDelta);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateButton(panel, OptionCheckLabel, OpenViewer);
        CreateButton(panel, CancelLabel, CloseOptionPopup, new Color(0.34f, 0.38f, 0.43f, 1f));
    }

    private void OpenViewer()
    {
        CloseOptionPopup();
        EnsureEventSystem();

        GameObject canvasPrefab = Resources.Load<GameObject>(viewerCanvasResourcePath);
        viewerRoot = canvasPrefab != null ? Instantiate(canvasPrefab) : new GameObject("DressingRoomViewCanvas", typeof(RectTransform));
        viewerRoot.name = "DressingRoomViewCanvas";

        activeCanvas = viewerRoot.GetComponent<Canvas>();
        if (activeCanvas == null)
        {
            activeCanvas = viewerRoot.AddComponent<Canvas>();
        }

        activeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        activeCanvas.sortingOrder = 110;

        if (viewerRoot.GetComponent<GraphicRaycaster>() == null)
        {
            viewerRoot.AddComponent<GraphicRaycaster>();
        }

        BuildViewer();
    }

    private void BuildViewer()
    {
        if (viewerRoot == null) return;

        ClearChildren(viewerRoot.transform);

        RectTransform canvasRect = viewerRoot.transform as RectTransform;
        if (canvasRect == null)
        {
            canvasRect = CreateUIRect("CanvasContent", viewerRoot.transform);
            canvasRect.sizeDelta = new Vector2(Screen.width, Screen.height);
        }

        Stretch(canvasRect);

        RectTransform backdrop = CreateUIRect("Backdrop", canvasRect);
        Stretch(backdrop);
        Image backdropImage = backdrop.gameObject.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panel = CreatePanel(backdrop, new Vector2(820f, 640f), new Color(0.10f, 0.12f, 0.15f, 0.98f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(22, 22, 18, 18);
        panelLayout.spacing = 12f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        RectTransform header = CreateUIRect("Header", panel);
        AddLayoutElement(header, -1, 48, 1, 0);
        HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;

        Text title = CreateText("Title", header, $"{ViewerTitle} ({cards.Count})", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(title.rectTransform, -1, -1, 1, 1);
        CreateButton(header, CancelLabel, CloseViewer, new Color(0.34f, 0.38f, 0.43f, 1f), 100f, 44f);

        RectTransform viewport = CreateUIRect("Viewport", panel);
        AddLayoutElement(viewport, -1, -1, 1, 1);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0.06f, 0.07f, 0.09f, 0.96f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 30f;

        RectTransform content = CreateUIRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(12f, 0f);
        content.offsetMax = new Vector2(-12f, 0f);

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(132f, 184f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        if (cards.Count == 0)
        {
            Text emptyText = CreateText("Empty", content, EmptyMessage, 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            AddLayoutElement(emptyText.rectTransform, 720f, 120f, 0, 0);
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CreateCardView(content, cards[i], i);
        }
    }

    private void CreateCardView(RectTransform parent, CardDefinition card, int index)
    {
        RectTransform item = CreateUIRect("DressingRoomCard_" + index, parent);

        Image image = item.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.preserveAspect = true;
        image.color = new Color(0.18f, 0.20f, 0.23f, 1f);

        DressingRoomViewerCard viewCard = item.gameObject.AddComponent<DressingRoomViewerCard>();
        viewCard.Initialize(this, index);
    }

    private void ShowMoveToHandConfirmation(int cardIndex)
    {
        if (!IsValidCardIndex(cardIndex) || viewerRoot == null) return;

        CloseConfirmation();
        pendingMoveToHandIndex = cardIndex;

        confirmationRoot = new GameObject("DressingRoomConfirmation", typeof(RectTransform));
        confirmationRoot.transform.SetParent(viewerRoot.transform, false);

        RectTransform backdrop = confirmationRoot.GetComponent<RectTransform>();
        Stretch(backdrop);
        Image backdropImage = confirmationRoot.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.35f);

        RectTransform panel = CreatePanel(backdrop, new Vector2(420f, 190f), new Color(0.13f, 0.15f, 0.18f, 1f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text message = CreateText("Message", panel, ConfirmMoveMessage, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddLayoutElement(message.rectTransform, -1, 70f, 1, 0);

        RectTransform row = CreateUIRect("Buttons", panel);
        AddLayoutElement(row, -1, 52f, 1, 0);
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;

        CreateButton(row, ConfirmLabel, ConfirmMoveToHand);
        CreateButton(row, CancelLabel, CloseConfirmation, new Color(0.34f, 0.38f, 0.43f, 1f));
    }

    private void ConfirmMoveToHand()
    {
        int cardIndex = pendingMoveToHandIndex;
        CloseConfirmation();
        MoveCardToHand(cardIndex);
    }

    private void MoveCardToHand(int cardIndex)
    {
        if (!IsValidCardIndex(cardIndex)) return;

        CardDefinition definition = cards[cardIndex];
        cards.RemoveAt(cardIndex);
        UpdateTopSprite();

        Vector3 spawnPosition = GetDressingRoomWorldPosition();
        Card card = CardCatalog.CreateCard(cardPrefab, definition, spawnPosition, Quaternion.identity);
        if (card == null) return;
        card.AddToHand();
        BuildViewer();
    }

    private void ResolveDressingRoomField()
    {
        if (dressingRoomField == null)
        {
            FieldRaycastHandler[] fields = Object.FindObjectsByType<FieldRaycastHandler>(FindObjectsSortMode.None);
            dressingRoomField = fields.FirstOrDefault(field => field.FieldPosition == dressingRoomPosition);
        }

        CacheFieldRenderer();
    }

    private void CacheFieldRenderer()
    {
        if (dressingRoomField == null) return;

        fieldSpriteRenderer = dressingRoomField.GetComponent<SpriteRenderer>();
        if (fieldSpriteRenderer != null && defaultFieldSprite == null)
        {
            defaultFieldSprite = fieldSpriteRenderer.sprite;
        }
    }

    private void UpdateTopSprite()
    {
        ResolveDressingRoomField();
        if (fieldSpriteRenderer == null) return;

        fieldSpriteRenderer.sprite = defaultFieldSprite;
    }

    private bool IsPointerOverDressingRoom(Vector2 screenPosition)
    {
        ResolveDressingRoomField();
        if (dressingRoomField == null || Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return false;

        return hit.transform == dressingRoomField.transform || hit.transform.IsChildOf(dressingRoomField.transform);
    }

    private bool IsPointerOverHand(Vector2 screenPosition)
    {
        return screenPosition.y <= Screen.height * handDropScreenHeightPercent;
    }

    private Vector3 GetDressingRoomWorldPosition()
    {
        ResolveDressingRoomField();
        if (dressingRoomField != null)
        {
            return dressingRoomField.transform.position;
        }

        return Vector3.zero;
    }

    private void CloseViewer()
    {
        CloseConfirmation();
        DestroyDragGhost();

        if (viewerRoot != null)
        {
            Destroy(viewerRoot);
            viewerRoot = null;
            activeCanvas = null;
        }
    }

    private void CloseOptionPopup()
    {
        if (optionPopupRoot == null) return;

        Destroy(optionPopupRoot);
        optionPopupRoot = null;
    }

    private void CloseConfirmation()
    {
        pendingMoveToHandIndex = -1;
        if (confirmationRoot == null) return;

        Destroy(confirmationRoot);
        confirmationRoot = null;
    }

    private void DestroyDragGhost()
    {
        if (dragGhost == null) return;

        Destroy(dragGhost);
        dragGhost = null;
    }

    private bool IsValidCardIndex(int cardIndex)
    {
        return cardIndex >= 0 && cardIndex < cards.Count;
    }

    private CardDefinition FindCardDefinition(int cardId)
    {
        return CardCatalog.TryGet(cardId, out CardDefinition definition) ? definition : null;
    }

    private void LoadCardLibrary()
    {
        cardPrefab = Resources.Load<Card>("Cards/cardPrefab");
        if (cardPrefab == null) Debug.LogError("Missing Resources/Cards/cardPrefab.prefab");
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private void EnsureActiveCanvas()
    {
        if (activeCanvas != null) return;

        if (viewerRoot == null)
        {
            OpenViewer();
            return;
        }

        activeCanvas = viewerRoot.GetComponent<Canvas>();
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

    private RectTransform CreatePanel(Transform parent, Vector2 size, Color color)
    {
        RectTransform panel = CreateUIRect("Panel", parent);
        panel.sizeDelta = size;

        Image image = panel.gameObject.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, Color? color = null, float width = -1f, float height = 48f)
    {
        RectTransform rect = CreateUIRect(label + "Button", parent);
        AddLayoutElement(rect, width, height, width < 0f ? 1f : 0f, 0f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color ?? new Color(0.82f, 0.18f, 0.28f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText("Label", rect, label, 19, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 8, 8, 4, 4);
        return button;
    }

    private Text CreateText(string name, RectTransform parent, string value, int size, FontStyle style, TextAnchor alignment)
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

    private RectTransform CreateUIRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void AddLayoutElement(RectTransform rect, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
    {
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
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

    private Vector2 ClampPopupPosition(Vector2 screenPosition, Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        return new Vector2(
            Mathf.Clamp(screenPosition.x, halfWidth, Screen.width - halfWidth),
            Mathf.Clamp(screenPosition.y, halfHeight, Screen.height - halfHeight));
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
