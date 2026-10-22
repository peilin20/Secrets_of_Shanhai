using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shows one EventData on a card. Clicking the card opens it full size.
public class EventCard : MonoBehaviour, IPointerClickHandler
{
    public EventData data;
    public bool openOnClick = true;   // Off for the full-size copy inside the event window.

    [Header("UI")]
    public Text titleText;
    public Image eventImage;
    public Text bodyText;

    void Start()
    {
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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
