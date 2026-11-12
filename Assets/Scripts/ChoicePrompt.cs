using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A simple question on top of everything: a message and one button per answer.
// Built the first time it's needed, on the main canvas. Use: yield return ChoicePrompt.Ask(...).
public class ChoicePrompt : MonoBehaviour
{
    static ChoicePrompt instance;

    Text message;
    RectTransform row;
    int answer = -1;

    static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f);
    static readonly Color Tan = new Color(0.88f, 0.82f, 0.68f);
    static readonly Color Ink = new Color(0.2f, 0.16f, 0.12f);

    // Shows the message with these answers and waits for a click; `result` gets the answer's index.
    public static IEnumerator Ask(string text, List<string> answers, System.Action<int> result)
    {
        var p = Get();
        if (!p) { result?.Invoke(0); yield break; }
        p.message.text = text;
        foreach (Transform child in p.row) Destroy(child.gameObject);
        p.answer = -1;
        // Buttons shrink so a long list still fits in the panel.
        float width = Mathf.Min(220f, (1060f - 12f * (answers.Count - 1)) / Mathf.Max(1, answers.Count));
        for (int i = 0; i < answers.Count; i++)
        {
            int index = i;
            p.MakeButton(answers[i], width).onClick.AddListener(() => p.answer = index);
        }
        p.gameObject.SetActive(true);
        p.transform.SetAsLastSibling();
        while (p.answer < 0) yield return null;
        p.gameObject.SetActive(false);
        result?.Invoke(p.answer);
    }

    // Like Ask, but the answers are cards: taller buttons in their own colours, with wrapped text
    // (e.g. a 负伤 card's element and effect). A dark colour gets white text.
    public static IEnumerator AskCards(string text, List<string> answers, List<Color> colors, System.Action<int> result)
    {
        var p = Get();
        if (!p) { result?.Invoke(0); yield break; }
        p.message.text = text;
        foreach (Transform child in p.row) Destroy(child.gameObject);
        p.answer = -1;
        float width = Mathf.Min(300f, (1060f - 12f * (answers.Count - 1)) / Mathf.Max(1, answers.Count));
        p.row.sizeDelta = new Vector2(-40f, 190f);
        p.message.rectTransform.offsetMin = new Vector2(40f, 230f);
        for (int i = 0; i < answers.Count; i++)
        {
            int index = i;
            var button = p.MakeButton(answers[i], width);
            ((RectTransform)button.transform).sizeDelta = new Vector2(width, 180f);
            var color = i < colors.Count ? colors[i] : Tan;
            button.GetComponent<Image>().color = color;
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = answers[i].Length > 60 ? 18 : 21;
            label.supportRichText = true;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.offsetMin = new Vector2(10f, 8f);
            label.rectTransform.offsetMax = new Vector2(-10f, -8f);
            float brightness = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
            label.color = brightness < 0.5f ? Color.white : Ink;
            button.onClick.AddListener(() => p.answer = index);
        }
        p.gameObject.SetActive(true);
        p.transform.SetAsLastSibling();
        while (p.answer < 0) yield return null;
        p.gameObject.SetActive(false);
        // Back to the normal layout for plain questions.
        p.row.sizeDelta = new Vector2(-40f, 64f);
        p.message.rectTransform.offsetMin = new Vector2(40f, 110f);
        result?.Invoke(p.answer);
    }

    static ChoicePrompt Get()
    {
        if (instance) return instance;
        // The table's canvas (the one the character cards are on).
        var card = FindFirstObjectByType<CharacterCard>();
        var canvas = card ? card.GetComponentInParent<Canvas>().rootCanvas : FindFirstObjectByType<Canvas>();
        if (!canvas) return null;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Dims the whole screen so nothing else can be clicked.
        var dim = new GameObject("ChoicePrompt", typeof(RectTransform), typeof(Image));
        var drt = (RectTransform)dim.transform;
        drt.SetParent(canvas.transform, false);
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        instance = dim.AddComponent<ChoicePrompt>();

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        var prt = (RectTransform)panel.transform;
        prt.SetParent(drt, false);
        prt.sizeDelta = new Vector2(1100f, 520f);
        panel.GetComponent<Image>().color = Paper;
        panel.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);

        var msg = new GameObject("Message", typeof(RectTransform), typeof(Text));
        var mrt = (RectTransform)msg.transform;
        mrt.SetParent(prt, false);
        mrt.anchorMin = new Vector2(0f, 0f); mrt.anchorMax = new Vector2(1f, 1f);
        mrt.offsetMin = new Vector2(40f, 110f); mrt.offsetMax = new Vector2(-40f, -30f);
        instance.message = msg.GetComponent<Text>();
        instance.message.font = font;
        instance.message.fontSize = 26;
        instance.message.color = Ink;
        instance.message.alignment = TextAnchor.MiddleCenter;
        instance.message.supportRichText = true;

        var buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        instance.row = (RectTransform)buttons.transform;
        instance.row.SetParent(prt, false);
        instance.row.anchorMin = new Vector2(0f, 0f); instance.row.anchorMax = new Vector2(1f, 0f);
        instance.row.pivot = new Vector2(0.5f, 0f);
        instance.row.anchoredPosition = new Vector2(0f, 30f);
        instance.row.sizeDelta = new Vector2(-40f, 64f);
        var layout = buttons.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false; layout.childControlHeight = false;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

        dim.SetActive(false);
        return instance;
    }

    Button MakeButton(string label, float width)
    {
        var go = new GameObject("Answer", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(row, false);
        rt.sizeDelta = new Vector2(width, 60f);
        go.GetComponent<Image>().color = Tan;
        var text = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var trt = (RectTransform)text.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6f, 0f); trt.offsetMax = new Vector2(-6f, 0f);
        var t = text.GetComponent<Text>();
        t.font = message.font;
        t.fontSize = 22;
        t.color = Ink;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
        return go.GetComponent<Button>();
    }
}
