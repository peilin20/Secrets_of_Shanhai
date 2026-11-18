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

    void OnEnable() { L.Changed += OnLanguageChanged; }
    void OnDisable() { L.Changed -= OnLanguageChanged; }

    void OnLanguageChanged()
    {
        if (eventsDrawn && drawButtonText) drawButtonText.text = L.T("story.eventsDrawn");
        else Refresh();
    }

    // The 下一步 line isn't shown: the game moves on by itself. The story text takes its room.
    public bool showNextStep = false;

    void Awake()
    {
        if (!showNextStep && nextStepText && bodyText)
        {
            nextStepText.gameObject.SetActive(false);
            var body = (RectTransform)bodyText.transform;
            body.offsetMin = new Vector2(body.offsetMin.x, body.offsetMin.y - ((RectTransform)nextStepText.transform).rect.height);
        }
    }

    void Start()
    {
        Refresh();
        if (drawButton) drawButton.onClick.AddListener(DrawEvents);
        if (dealOnStart) StartCoroutine(DealIn());
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
            drawButtonText.text = L.T("story.eventsDrawn");
        }
        else
        {
            drawButtonText.text = deck.CardsLeft == 0 ? L.T("story.noEventsLeft") : L.T("story.slotsFull");
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
        if (holdStory) return;
        StartCoroutine(AdvanceNextFrame());
    }

    // Set by the tutorial: its practice events don't move the story on.
    [System.NonSerialized] public bool holdStory;

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
        StartCoroutine(FlipTo(story.nextStory));
        return true;
    }

    // --- Turning a main story card: the moment every player gets 1 primary token ---

    [Header("Animation")]
    public bool dealOnStart = true;    // The game's first main story card is dealt onto the table when the scene starts.
    public float dealTime = 0.7f;
    public float flipTime = 0.25f;

    // The first card flies in from above, turning over, into its place; then the tokens are handed out.
    System.Collections.IEnumerator DealIn()
    {
        var rt = (RectTransform)transform;
        Vector2 target = rt.anchoredPosition;
        Vector3 scale = rt.localScale;
        Vector2 start = target + new Vector2(0f, 1100f);
        for (float t = 0f; t < dealTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dealTime);
            rt.anchoredPosition = Vector2.Lerp(start, target, k);
            rt.localScale = new Vector3(scale.x * Mathf.Abs(Mathf.Cos((1f - k) * Mathf.PI)), scale.y, scale.z) * Mathf.Lerp(0.5f, 1f, k);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-12f, 0f, k));
            yield return null;
        }
        rt.anchoredPosition = target;
        rt.localScale = scale;
        rt.localEulerAngles = Vector3.zero;
        yield return OnNewStory(false);
    }

    // The card turns over to the next main story card; then the tokens are handed out.
    System.Collections.IEnumerator FlipTo(StoryData next)
    {
        var rt = (RectTransform)transform;
        Vector3 scale = rt.localScale;
        for (float t = 0f; t < flipTime; t += Time.deltaTime)
        {
            rt.localScale = new Vector3(scale.x * (1f - t / flipTime), scale.y, scale.z);
            yield return null;
        }
        SetStory(next);
        for (float t = 0f; t < flipTime; t += Time.deltaTime)
        {
            rt.localScale = new Vector3(scale.x * (t / flipTime), scale.y, scale.z);
            yield return null;
        }
        rt.localScale = scale;
        yield return OnNewStory(true);
    }

    // A main story card has been turned: every player gets its number of primary-element tokens (负伤 may change that).
    // On later cards, the 负伤 that heal on reaching the next main story card are settled too.
    System.Collections.IEnumerator OnNewStory(bool reachedNext)
    {
        var injuries = InjuryDeck.Find();
        var prefab = injuries ? injuries.tokenPrefab : null;
        if (prefab)
        {
            // Each player's token flies from the token pool to their card (no message: the tutorial explains it).
            Canvas.ForceUpdateCanvases();
            var cards = new System.Collections.Generic.List<CharacterCard>(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
            cards.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            // How many depends on the card (主线牌 sheet: 每位玩家主属性token获取数量).
            int n = story ? story.primaryTokens : 1;
            for (int i = 0; i < n; i++) yield return TokenFlight.GiveEach(cards, p => p.primaryElement, prefab);
        }
        if (injuries && reachedNext) yield return injuries.OnNewStory();
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
                drawButtonText.text = n == 1 ? L.T("story.drawEvent") : L.T("story.drawEvents", n);
        }
    }
}
