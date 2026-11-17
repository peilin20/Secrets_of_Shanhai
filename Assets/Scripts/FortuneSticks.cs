using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 1d6 roll as a fortune-stick box (抽签): click the box, it shakes, and a wooden stick rises with the number.
public class FortuneSticks : MonoBehaviour
{
    public int sides = 6;

    [Header("UI")]
    public Button boxButton;
    public RectTransform body;      // Shaken as a whole (box + sticks).
    public RectTransform stick;     // The stick that comes out.
    public Text numberText;

    [Header("Animation")]
    public float shakeTime = 0.5f;
    public float shakeAngle = 8f;
    public float riseTime = 0.35f;
    public float hiddenY = 0f;
    public float shownY = 120f;

    public int LastResult { get; private set; }

    bool rolling;

    void Awake()
    {
        if (boxButton) boxButton.onClick.AddListener(OnBoxClicked);
    }

    void Start()
    {
        SetStickY(hiddenY);
        numberText.text = "";
    }

    public bool IsRolling => rolling;

    System.Action<int> awaiting;   // Set while 承天问卜 waits for the player to click the box.
    int awaitingSides;

    void OnBoxClicked()
    {
        if (rolling) return;
        if (awaiting == null) { Roll(); return; }
        var done = awaiting;
        awaiting = null;
        int normal = sides;
        sides = awaitingSides;
        Roll(r => { sides = normal; done(r); });
    }

    // --- 承天问卜: every die a combat / social needs ---
    // The screen dims, the stick box moves to the middle under the title 承天问卜, and the player clicks it to draw.
    // The drawn number is shown for a moment, then the box goes back to the table.
    public static float resultPause = 1f;

    public static IEnumerator Ask(int sides, System.Action<int> result)
    {
        var sticks = FindFirstObjectByType<FortuneSticks>();
        var canvas = sticks ? sticks.GetComponentInParent<Canvas>() : null;
        if (!sticks || !canvas) { result(Random.Range(1, sides + 1)); yield break; }
        while (sticks.rolling) yield return null;
        // The last stick goes back in the box before this draw.
        sticks.numberText.text = "";
        sticks.SetStickY(sticks.hiddenY);
        canvas = canvas.rootCanvas;
        var font = sticks.numberText ? sticks.numberText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Dim layer over everything (it also blocks clicks on the rest of the screen).
        var overlay = new GameObject("FortuneAsk", typeof(RectTransform), typeof(Image));
        var ort = (RectTransform)overlay.transform;
        ort.SetParent(canvas.transform, false);
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.93f);
        ort.SetAsLastSibling();

        var paper = new Color(0.96f, 0.92f, 0.82f);
        var title = MakeText(ort, font, L.T("fortune.title"), 72, paper, new Vector2(0f, 440f), new Vector2(900f, 110f));
        title.fontStyle = FontStyle.Bold;
        var hint = MakeText(ort, font, L.T("fortune.hint", sides), 30, paper, new Vector2(0f, -360f), new Vector2(1000f, 60f));

        // Borrow the table's stick box.
        var rt = (RectTransform)sticks.transform;
        var parent = rt.parent;
        int index = rt.GetSiblingIndex();
        Vector2 aMin = rt.anchorMin, aMax = rt.anchorMax, pivot = rt.pivot, pos = rt.anchoredPosition;
        Vector3 scale = rt.localScale;
        rt.SetParent(ort, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 70f);   // The box art sits below its pivot.
        rt.localScale = scale * 1.8f;

        int got = 0;
        bool done = false;
        sticks.awaitingSides = sides;
        sticks.awaiting = r => { got = r; done = true; };
        while (!done && sticks) yield return null;
        if (!sticks) { if (overlay) Destroy(overlay); result(Random.Range(1, sides + 1)); yield break; }

        hint.text = L.T("fortune.result", got);
        yield return new WaitForSecondsRealtime(resultPause);

        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.localScale = scale;
        rt.SetSiblingIndex(index);
        Destroy(overlay);
        result(got);
    }

    static Text MakeText(RectTransform parent, Font font, string text, int size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    public void Roll()
    {
        if (!rolling) StartCoroutine(RollRoutine(null));
    }

    // Rolls and reports the drawn number when the stick is out (used by combat).
    public void Roll(System.Action<int> onDone)
    {
        if (!rolling) StartCoroutine(RollRoutine(onDone));
    }

    IEnumerator RollRoutine(System.Action<int> onDone)
    {
        rolling = true;

        // Put the last stick back in the box.
        numberText.text = "";
        SetStickY(hiddenY);

        // Shake, fading out.
        for (float t = 0f; t < shakeTime; t += Time.deltaTime)
        {
            float fade = 1f - t / shakeTime;
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 45f) * shakeAngle * fade);
            yield return null;
        }
        body.localRotation = Quaternion.identity;

        LastResult = Random.Range(1, sides + 1);
        numberText.text = LastResult.ToString();

        // Stick rises out of the box.
        for (float t = 0f; t < riseTime; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / riseTime, 3f);
            SetStickY(Mathf.Lerp(hiddenY, shownY, k));
            yield return null;
        }
        SetStickY(shownY);

        rolling = false;
        onDone?.Invoke(LastResult);
    }

    void SetStickY(float y)
    {
        stick.anchoredPosition = new Vector2(stick.anchoredPosition.x, y);
    }
}
