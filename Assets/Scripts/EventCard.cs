using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shows one EventData on a card. A card dealt from the pile arrives face down: the first click turns it over
// (a flip animation), and clicking the face-up card opens it full size.
public class EventCard : MonoBehaviour, IPointerClickHandler
{
    public EventData data;
    public bool openOnClick = true;   // Off for the full-size copy inside the event window.

    [Header("UI")]
    public Text titleText;
    public Image eventImage;
    public Text bodyText;

    [Header("Back")]
    public float flipTime = 0.3f;
    public Color backColor = new Color(0.36f, 0.27f, 0.2f);

    public bool FaceDown => back && back.activeSelf;
    GameObject back;
    bool flipping;

    void Start()
    {
        Refresh();
    }

    // Covers the card with its back (made the first time).
    public void SetFaceDown(bool down)
    {
        if (down && !back)
        {
            back = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Outline));
            var rt = (RectTransform)back.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            back.GetComponent<Image>().color = backColor;
            var outline = back.GetComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.7f, 0.4f);
            outline.effectDistance = new Vector2(4f, -4f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LocalizedText));
            var lrt = (RectTransform)label.transform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            text.font = titleText ? titleText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.85f, 0.6f);
            text.raycastTarget = false;
            text.text = L.T("card.eventBack");
            label.GetComponent<LocalizedText>().key = "card.eventBack";
        }
        if (back)
        {
            back.SetActive(down);
            back.transform.SetAsLastSibling();
        }
    }

    bool EarlierCardFaceDown()
    {
        var deck = FindFirstObjectByType<EventDeck>();
        if (!deck) return false;
        foreach (var slot in deck.slots)
        {
            if (slot == transform.parent) return false;
            if (slot.childCount == 0) continue;
            var other = slot.GetChild(0).GetComponent<EventCard>();
            if (other && other != this && other.FaceDown) return true;
        }
        return false;
    }

    System.Collections.IEnumerator Shake()
    {
        flipping = true;
        var rt = (RectTransform)transform;
        var home = rt.anchoredPosition;
        for (float e = 0f; e < 0.3f; e += Time.deltaTime)
        {
            rt.anchoredPosition = home + new Vector2(Mathf.Sin(e * 60f) * 8f * (1f - e / 0.3f), 0f);
            yield return null;
        }
        rt.anchoredPosition = home;
        flipping = false;
    }

    // The card turns over: it narrows to nothing, shows its other side, and widens again.
    System.Collections.IEnumerator Flip()
    {
        flipping = true;
        var t = transform;
        float width = t.localScale.x;
        for (float e = 0f; e < flipTime * 0.5f; e += Time.deltaTime)
        {
            t.localScale = new Vector3(width * (1f - e / (flipTime * 0.5f)), t.localScale.y, t.localScale.z);
            yield return null;
        }
        SetFaceDown(false);
        for (float e = 0f; e < flipTime * 0.5f; e += Time.deltaTime)
        {
            t.localScale = new Vector3(width * (e / (flipTime * 0.5f)), t.localScale.y, t.localScale.z);
            yield return null;
        }
        t.localScale = new Vector3(width, t.localScale.y, t.localScale.z);
        flipping = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (flipping) return;
        if (FaceDown)
        {
            // Cards are turned over in order: not while an earlier one is still face down (it shakes instead).
            if (EarlierCardFaceDown()) StartCoroutine(Shake());
            else StartCoroutine(Flip());   // First click: turn it over.
            return;
        }
        if (!openOnClick || !data) return;
        var popup = FindFirstObjectByType<EventPopup>(FindObjectsInactive.Include);
        if (popup) popup.Show(this);
    }

    void OnValidate()
    {
        Refresh();
    }

    public void SetEvent(EventData eventData)
    {
        data = eventData;
        Refresh();
    }

    public void Refresh()
    {
        if (!data) return;
        if (titleText) titleText.text = data.title;
        if (eventImage)
        {
            // Crops to fill the frame instead of stretching.
            eventImage.sprite = data.image;
            eventImage.enabled = data.image != null;
            var fitter = eventImage.GetComponent<AspectRatioFitter>();
            if (fitter && data.image) fitter.aspectRatio = data.image.rect.width / data.image.rect.height;
        }
        if (bodyText)
        {
            var sb = new StringBuilder(data.description);
            if (data.options.Count > 0) sb.Append("\n");
            foreach (var option in data.options)
                // No space after the label: Unity's text wraps at spaces, which would push
                // the whole Chinese sentence onto the next line.
                sb.Append("\n<b>").Append(option.label).Append("</b>").Append(option.text);
            if (!string.IsNullOrEmpty(data.nextStep)) sb.Append("\n\n<b>").Append(data.nextStep).Append("</b>");
            bodyText.text = sb.ToString();
        }
    }
}
