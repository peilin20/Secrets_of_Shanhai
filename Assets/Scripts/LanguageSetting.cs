using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Every time a scene loads: adds a "设置 / Settings" button in the top-right corner (it opens a small panel
// with the language switch), and hooks up the scene's fixed labels so they follow the language setting.
public static class LanguageSetting
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Setup();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup();
    }

    static void Setup()
    {
        foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.GetComponent<LocalizedText>()) continue;
            string key = L.SceneLabelKey(text.text);
            if (key != null) text.gameObject.AddComponent<LocalizedText>().key = key;
        }
        if (!GameObject.Find("SettingsOverlay")) CreateButton();
    }

    static void CreateButton()
    {
        var canvasGo = new GameObject("SettingsOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var open = MakeButton(canvasGo.transform, "SettingsButton", "settings.title", new Vector2(0f, 0f), new Vector2(12f, 12f),
            new Vector2(200f, 50f), new Color(0.15f, 0.12f, 0.1f, 0.75f), Color.white, 24);

        // The panel: a dim backdrop over the whole screen, a parchment box in the middle.
        var backdrop = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(canvasGo.transform, false);
        var brt = (RectTransform)backdrop.transform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(backdrop.transform, false);
        ((RectTransform)box.transform).sizeDelta = new Vector2(640f, 420f);
        box.GetComponent<Image>().color = new Color(0.93f, 0.89f, 0.8f);
        var ink = new Color(0.15f, 0.12f, 0.1f);
        var title = MakeLabel(box.transform, "settings.title", 44, ink);
        var trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = Vector2.one;
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, 90f);
        trt.anchoredPosition = new Vector2(0f, -20f);
        var paper = new Color(0.82f, 0.76f, 0.62f);
        var language = MakeButton(box.transform, "LanguageButton", "settings.language", new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
            new Vector2(380f, 80f), paper, ink, 32);
        language.onClick.AddListener(L.Toggle);
        var close = MakeButton(box.transform, "CloseButton", "settings.close", new Vector2(0.5f, 0.5f), new Vector2(0f, -130f),
            new Vector2(240f, 70f), paper, ink, 30);

        backdrop.SetActive(false);
        open.onClick.AddListener(() => backdrop.SetActive(true));
        close.onClick.AddListener(() => backdrop.SetActive(false));
    }

    static Button MakeButton(Transform parent, string name, string key, Vector2 anchor, Vector2 position, Vector2 size,
        Color color, Color textColor, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        MakeLabel(go.transform, key, fontSize, textColor);
        return go.GetComponent<Button>();
    }

    static Text MakeLabel(Transform parent, string key, int fontSize, Color color)
    {
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = label.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.text = L.T(key);
        label.AddComponent<LocalizedText>().key = key;
        return t;
    }
}
