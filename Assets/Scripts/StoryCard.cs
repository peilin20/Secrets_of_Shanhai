using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shows one StoryData on a card. The card can be dragged anywhere on the table (never off it),
// and snaps into the story slot when dropped over it.
public class StoryCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public StoryData story;

    [Header("UI")]
    public Text titleText;
    public Image storyImage;
    public Text subtitleText;
    public Text bodyText;
    public Text nextStepText;
    public Button drawButton;
    public Text drawButtonText;

    bool eventsDrawn;

    void Start()
    {
        Refresh();
        if (drawButton) drawButton.onClick.AddListener(DrawEvents);
    }

    // Draws this story's event cards into the event slots. Each story card draws once.
    public void DrawEvents()
    {
        if (!story || eventsDrawn) return;
        var deck = FindFirstObjectByType<EventDeck>();
        if (!deck) return;

        int drawn = story.fixedEvents.Count > 0 ? deck.DrawSpecific(story.fixedEvents) : deck.Draw(story.eventCardsToDraw);
        if (drawn > 0)
        {
            eventsDrawn = true;
            drawButton.interactable = false;
            drawButtonText.text = "Event Cards Drawn";
        }
        else
        {
            drawButtonText.text = deck.CardsLeft == 0 ? "No event cards left" : "Event slots are full";
        }
    }

    void OnValidate()
    {
        Refresh();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        var parent = transform.parent as RectTransform;
        if (!parent) return;
        var rt = (RectTransform)transform;
        Vector3 pos = rt.localPosition + (Vector3)(eventData.delta / parent.lossyScale.x);

        // Keep the whole card on the table.
        Rect table = parent.rect;
        Vector2 size = rt.rect.size * rt.localScale.x;
        pos.x = Mathf.Clamp(pos.x, table.xMin + size.x * rt.pivot.x, table.xMax - size.x * (1 - rt.pivot.x));
        pos.y = Mathf.Clamp(pos.y, table.yMin + size.y * rt.pivot.y, table.yMax - size.y * (1 - rt.pivot.y));
        rt.localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Snap into the story slot if the card's center is over it.
        var slot = FindFirstObjectByType<StorySlot>();
        if (!slot || slot.transform.parent != transform.parent) return;
        var slotRect = (RectTransform)slot.transform;
        Vector2 center = slotRect.parent.InverseTransformPoint(transform.position);
        Vector2 slotCenter = slotRect.localPosition;
        Vector2 half = slotRect.rect.size * 0.5f;
        if (Mathf.Abs(center.x - slotCenter.x) <= half.x && Mathf.Abs(center.y - slotCenter.y) <= half.y)
            transform.localPosition = slotRect.localPosition;
    }

    public void SetStory(StoryData data)
    {
        story = data;
        eventsDrawn = false;
        if (drawButton) drawButton.interactable = true;
        Refresh();
    }

    // Called after an event is dealt with: once no event cards are left on the table,
    // turn to the next main story card.
    public void AdvanceWhenEventsDone()
    {
        StartCoroutine(AdvanceNextFrame());
    }

    System.Collections.IEnumerator AdvanceNextFrame()
    {
        yield return null;   // Let the resolved event card actually be destroyed first.
        var deck = FindFirstObjectByType<EventDeck>();
        if (!deck || deck.EventsOnTable == 0) ShowNextStory();
    }

    // Turns to the next main story card. Returns false if there isn't one yet.
    public bool ShowNextStory()
    {
        if (!story || !story.nextStory) return false;
        SetStory(story.nextStory);
        // Some 负伤 heal when the team reaches the next main story card.
        var injuries = InjuryDeck.Find();
        if (injuries) injuries.StartCoroutine(injuries.OnNewStory());
        return true;
    }

    // A long story text gets a smaller font so it never runs into the 下一步 line.
    public int bodyMaxFontSize = 18;
    public int bodyMinFontSize = 12;

    void FitBody()
    {
        var rect = ((RectTransform)bodyText.transform).rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        for (int size = bodyMaxFontSize; size >= bodyMinFontSize; size--)
        {
            bodyText.fontSize = size;
            var settings = bodyText.GetGenerationSettings(new Vector2(rect.width, 0f));
            float height = bodyText.cachedTextGeneratorForLayout.GetPreferredHeight(bodyText.text, settings) / bodyText.pixelsPerUnit;
            if (height <= rect.height) break;
        }
    }

    public void Refresh()
    {
        if (!story) return;
        if (titleText) titleText.text = story.number + " " + story.title;
        if (storyImage)
        {
            // Crops to fill the frame instead of stretching.
            storyImage.sprite = story.image;
            storyImage.enabled = story.image != null;
            var fitter = storyImage.GetComponent<AspectRatioFitter>();
            if (fitter && story.image) fitter.aspectRatio = story.image.rect.width / story.image.rect.height;
        }
        if (subtitleText) subtitleText.text = story.subtitle;
        if (bodyText)
        {
            bodyText.text = story.body;
            FitBody();
        }
        if (nextStepText) nextStepText.text = story.nextStep;
        if (drawButton)
        {
            int n = story.fixedEvents.Count > 0 ? story.fixedEvents.Count : story.eventCardsToDraw;
            // Hidden when the story draws no events (SetActive isn't allowed from OnValidate, so only in play).
            if (Application.isPlaying) drawButton.gameObject.SetActive(n > 0);
            if (drawButtonText && !eventsDrawn)
                drawButtonText.text = "Draw " + n + " Event Card" + (n == 1 ? "" : "s");
        }
    }
}
