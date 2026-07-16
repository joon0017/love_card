using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private static Font font;
    private static readonly Color Navy = new Color(0.025f, 0.045f, 0.085f, 1f);
    private static readonly Color Panel = new Color(0.055f, 0.085f, 0.14f, 0.97f);
    private static readonly Color Gold = new Color(0.94f, 0.67f, 0.20f, 1f);
    private static readonly Color Blue = new Color(0.10f, 0.32f, 0.54f, 1f);

    [MenuItem("Tools/Main Menu/Rebuild Editable Scene")]
    public static void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.GetComponent<Image>().color = Navy;

        RectTransform decoration = Rect("BackgroundGlow", canvasObject.transform);
        Stretch(decoration, 70, 70, 70, 70);
        Image glow = decoration.gameObject.AddComponent<Image>();
        glow.color = new Color(0.05f, 0.19f, 0.31f, 0.48f);
        glow.raycastTarget = false;

        RectTransform content = Rect("Content", canvasObject.transform);
        content.anchorMin = new Vector2(0.12f, 0.10f);
        content.anchorMax = new Vector2(0.88f, 0.90f);
        content.offsetMin = content.offsetMax = Vector2.zero;

        RectTransform brand = Rect("Brand", content);
        brand.anchorMin = new Vector2(0, 0.18f);
        brand.anchorMax = new Vector2(0.58f, 0.82f);
        brand.offsetMin = brand.offsetMax = Vector2.zero;
        Text eyebrow = Label("Eyebrow", brand, "BUILD YOUR DECK. PLAY YOUR STORY.", 22, FontStyle.Bold, TextAnchor.LowerLeft, Gold);
        eyebrow.rectTransform.anchorMin = new Vector2(0, 0.72f); eyebrow.rectTransform.anchorMax = new Vector2(1, 0.82f); eyebrow.rectTransform.offsetMin = eyebrow.rectTransform.offsetMax = Vector2.zero;
        Text title = Label("GameTitle", brand, "LOVECA", 104, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        title.rectTransform.anchorMin = new Vector2(0, 0.39f); title.rectTransform.anchorMax = new Vector2(1, 0.72f); title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
        Text subtitle = Label("Subtitle", brand, "A tactical card game", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.72f, 0.80f, 0.90f));
        subtitle.rectTransform.anchorMin = new Vector2(0, 0.22f); subtitle.rectTransform.anchorMax = new Vector2(1, 0.42f); subtitle.rectTransform.offsetMin = subtitle.rectTransform.offsetMax = Vector2.zero;

        RectTransform menu = PanelRect("MenuPanel", content, Panel);
        menu.anchorMin = new Vector2(0.64f, 0.05f); menu.anchorMax = new Vector2(1f, 0.95f); menu.offsetMin = menu.offsetMax = Vector2.zero;
        VerticalLayoutGroup menuLayout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
        menuLayout.padding = new RectOffset(42, 42, 48, 48); menuLayout.spacing = 18;
        menuLayout.childControlWidth = true; menuLayout.childControlHeight = true; menuLayout.childForceExpandHeight = false;
        Text menuTitle = Label("MenuTitle", menu, "MAIN MENU", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.68f, 0.77f, 0.88f));
        Layout(menuTitle.rectTransform, -1, 56);
        Button play = Button("PlayButton", menu, "PLAY", Gold, new Color(0.08f, 0.07f, 0.04f), 78);
        Button editDeck = Button("EditDeckButton", menu, "EDIT DECK", Blue, Color.white, 70);
        Button settings = Button("SettingsButton", menu, "SETTINGS", Blue, Color.white, 70);
        Button exit = Button("ExitButton", menu, "EXIT", new Color(0.14f, 0.17f, 0.22f), Color.white, 70);
        Text version = Label("Version", menu, "EARLY BUILD", 16, FontStyle.Normal, TextAnchor.LowerRight, new Color(0.42f, 0.50f, 0.60f));
        Layout(version.rectTransform, -1, 40, 1);

        GameObject settingsRoot = BuildSettingsPanel(canvasObject.transform, out Slider volume, out Toggle fullscreen, out Button close);
        MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
        controller.ConfigureEditorReferences(play, editDeck, settings, exit, settingsRoot, volume, fullscreen, close);
        settingsRoot.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Main Menu scene created and added first in Build Settings.");
    }

    private static GameObject BuildSettingsPanel(Transform parent, out Slider volume, out Toggle fullscreen, out Button close)
    {
        RectTransform overlay = PanelRect("SettingsOverlay", parent, new Color(0.01f, 0.02f, 0.04f, 0.82f)); Stretch(overlay);
        RectTransform dialog = PanelRect("SettingsDialog", overlay, Panel);
        dialog.anchorMin = new Vector2(0.34f, 0.25f); dialog.anchorMax = new Vector2(0.66f, 0.75f); dialog.offsetMin = dialog.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 36, 36); layout.spacing = 20; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        Text title = Label("Title", dialog, "SETTINGS", 38, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white); Layout(title.rectTransform, -1, 62);
        Text volumeLabel = Label("VolumeLabel", dialog, "MASTER VOLUME", 19, FontStyle.Bold, TextAnchor.LowerLeft, new Color(0.72f, 0.80f, 0.90f)); Layout(volumeLabel.rectTransform, -1, 38);
        volume = Slider("MasterVolume", dialog); Layout(volume.GetComponent<RectTransform>(), -1, 48);
        fullscreen = Toggle("FullscreenToggle", dialog, "Fullscreen"); Layout(fullscreen.GetComponent<RectTransform>(), -1, 58);
        close = Button("CloseButton", dialog, "DONE", Gold, new Color(0.08f, 0.07f, 0.04f), 66);
        return overlay.gameObject;
    }

    private static Slider Slider(string name, Transform parent)
    {
        RectTransform root = Rect(name, parent); Slider slider = root.gameObject.AddComponent<Slider>();
        RectTransform bg = PanelRect("Background", root, new Color(0.15f, 0.20f, 0.27f)); Stretch(bg, 0, 0, 17, 17);
        RectTransform fillArea = Rect("Fill Area", root); Stretch(fillArea, 6, 6, 19, 19);
        RectTransform fill = PanelRect("Fill", fillArea, Gold); Stretch(fill);
        RectTransform handleArea = Rect("Handle Slide Area", root); Stretch(handleArea, 10, 10, 10, 10);
        RectTransform handle = PanelRect("Handle", handleArea, Color.white); handle.sizeDelta = new Vector2(26, 26);
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        return slider;
    }

    private static Toggle Toggle(string name, Transform parent, string text)
    {
        RectTransform root = Rect(name, parent); Toggle toggle = root.gameObject.AddComponent<Toggle>();
        RectTransform box = PanelRect("Background", root, new Color(0.15f, 0.20f, 0.27f)); box.anchorMin = box.anchorMax = new Vector2(0, .5f); box.sizeDelta = new Vector2(32, 32); box.anchoredPosition = new Vector2(16, 0);
        RectTransform check = PanelRect("Checkmark", box, Gold); Stretch(check, 6, 6, 6, 6);
        Text label = Label("Label", root, text, 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white); Stretch(label.rectTransform, 48, 0, 0, 0);
        toggle.targetGraphic = box.GetComponent<Image>(); toggle.graphic = check.GetComponent<Image>(); toggle.isOn = true; return toggle;
    }

    private static Button Button(string name, Transform parent, string text, Color color, Color textColor, float height)
    {
        RectTransform root = PanelRect(name, parent, color); Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>(); Layout(root, -1, height);
        Text label = Label("Label", root, text, 24, FontStyle.Bold, TextAnchor.MiddleCenter, textColor); Stretch(label.rectTransform, 12, 12, 6, 6); return button;
    }

    private static Text Label(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color)
    { RectTransform root = Rect(name, parent); Text text = root.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.fontStyle = style; text.alignment = alignment; text.color = color; return text; }
    private static RectTransform PanelRect(string name, Transform parent, Color color) { RectTransform r = Rect(name, parent); r.gameObject.AddComponent<Image>().color = color; return r; }
    private static RectTransform Rect(string name, Transform parent) { GameObject go = new GameObject(name, typeof(RectTransform)); RectTransform r = go.GetComponent<RectTransform>(); r.SetParent(parent, false); return r; }
    private static void Stretch(RectTransform r, float left = 0, float right = 0, float top = 0, float bottom = 0) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); }
    private static void Layout(RectTransform r, float width, float height, float flexibleHeight = 0) { LayoutElement e = r.gameObject.GetComponent<LayoutElement>() ?? r.gameObject.AddComponent<LayoutElement>(); if (width >= 0) e.preferredWidth = width; if (height >= 0) e.preferredHeight = height; e.flexibleHeight = flexibleHeight; }
    private static void AddToBuildSettings() { EditorBuildSettingsScene main = new EditorBuildSettingsScene(ScenePath, true); EditorBuildSettings.scenes = new[] { main }.Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray(); }
}
