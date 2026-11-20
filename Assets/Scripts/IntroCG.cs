using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The opening text (Intro scene, after 开始游戏): each paragraph fades in, stays a while, fades out, then the next.
// Space / Enter: finish the fade-in at once, or move on to the next paragraph. 跳过 goes straight on.
// The texts are Localization keys intro.1 … intro.N (Chinese and English). Then character creation loads.
public class IntroCG : MonoBehaviour
{
    public string nextScene = "CharacterCreation";
    public int paragraphs = 4;
    public float fadeIn = 1.2f;
    public float fadeOut = 0.8f;
    public float holdBase = 2.5f;       // Seconds each paragraph stays…
    public float holdPerChar = 0.06f;   // …plus this much per character (longer text, longer read).
    public Color background = new Color(0.949f, 0.957f, 0.953f);   // The cover's background.
    public Color ink = new Color(0.12f, 0.1f, 0.08f);

    Text text;
    int current;   // The paragraph on screen (for a language change mid-way).
    bool skipRequested;
    bool leaving;

    void OnEnable() { L.Changed += OnLanguage; }
    void OnDisable() { L.Changed -= OnLanguage; }
    void OnLanguage() { if (text && current > 0) text.text = L.T("intro." + current); }

    void Start()
    {
        Build();
        StartCoroutine(Play());
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            skipRequested = true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !OverButton()) skipRequested = true;
    }

    IEnumerator Play()
    {
        for (int i = 1; i <= paragraphs; i++)
        {
            current = i;
            text.text = L.T("intro." + i);
            skipRequested = false;
            // Fade in (a key press shows it fully at once).
            for (float t = 0f; t < fadeIn && !skipRequested; t += Time.deltaTime) { SetAlpha(t / fadeIn); yield return null; }
            SetAlpha(1f);
            skipRequested = false;
            // Stay (a key press moves on).
            float hold = holdBase + holdPerChar * text.text.Length;
            for (float t = 0f; t < hold && !skipRequested; t += Time.deltaTime) yield return null;
            skipRequested = false;
            // Fade out.
            for (float t = 0f; t < fadeOut; t += Time.deltaTime) { SetAlpha(1f - t / fadeOut); yield return null; }
            SetAlpha(0f);
        }
        Leave();
    }

    void Leave()
    {
        if (leaving) return;
        leaving = true;
        SceneManager.LoadScene(nextScene);
    }

    void SetAlpha(float a)
    {
        var c = ink;
        c.a = Mathf.Clamp01(a);
        text.color = c;
    }

    Button skip;

    bool OverButton()
    {
        if (!skip) return false;
        var rt = (RectTransform)skip.transform;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Mouse.current.position.ReadValue(), null);
    }

    void Build()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasGo = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRt = (RectTransform)bg.transform;
        bgRt.SetParent(canvasGo.transform, false);
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = background;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(canvasGo.transform, false);
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1300f, 600f);
        text = textGo.GetComponent<Text>();
        text.font = font;
        text.fontSize = 38;
        text.lineSpacing = 1.4f;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        SetAlpha(0f);

        var hint = new GameObject("Hint", typeof(RectTransform), typeof(Text), typeof(LocalizedText));
        var hrt = (RectTransform)hint.transform;
        hrt.SetParent(canvasGo.transform, false);
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 50f);
        hrt.sizeDelta = new Vector2(1000f, 40f);
        var ht = hint.GetComponent<Text>();
        ht.font = font;
        ht.fontSize = 24;
        ht.alignment = TextAnchor.MiddleCenter;
        ht.color = new Color(ink.r, ink.g, ink.b, 0.45f);
        ht.raycastTarget = false;
        ht.text = L.T("intro.hint");
        hint.GetComponent<LocalizedText>().key = "intro.hint";

        var skipGo = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var srt = (RectTransform)skipGo.transform;
        srt.SetParent(canvasGo.transform, false);
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1f, 0f);
        srt.anchoredPosition = new Vector2(-40f, 40f);
        srt.sizeDelta = new Vector2(200f, 70f);
        skipGo.GetComponent<Image>().color = background;
        skip = skipGo.GetComponent<Button>();
        skip.onClick.AddListener(Leave);
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LocalizedText));
        var lrt = (RectTransform)label.transform;
        lrt.SetParent(srt, false);
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lt = label.GetComponent<Text>();
        lt.font = font;
        lt.fontSize = 32;
        lt.fontStyle = FontStyle.Bold;
        lt.color = Color.black;
        lt.alignment = TextAnchor.MiddleCenter;
        lt.raycastTarget = false;
        lt.text = L.T("intro.skip");
        label.GetComponent<LocalizedText>().key = "intro.skip";
    }
}
