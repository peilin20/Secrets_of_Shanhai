using UnityEngine;
using UnityEngine.UI;

// Full-size view of an event card, with one button per option.
// Combat options open the combat window, social options the social window. Only the close button closes the window.
public class EventPopup : MonoBehaviour
{
    public EventCard cardView;
    public RectTransform optionRow;
    public Button optionButtonTemplate;   // Inactive; copied once per option.
    public Button closeButton;
    public Color combatColor = new Color(0.86f, 0.55f, 0.45f);
    public Color socialColor = new Color(0.55f, 0.7f, 0.86f);
    public Color lootColor = new Color(0.95f, 0.78f, 0.35f);

    EventCard source;   // The event card on the table that this window is showing.

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
    }

    public void Show(EventCard card)
    {
        source = card;
        Show(card.data);
    }

    // The shown event has been dealt with: take its card off the table.
    public void ResolveCurrentEvent()
    {
        if (source) Destroy(source.gameObject);
        source = null;
    }

    public void Show(EventData data)
    {
        if (!data) return;
        cardView.SetEvent(data);

        foreach (Transform child in optionRow)
            if (child != optionButtonTemplate.transform) Destroy(child.gameObject);

        foreach (var option in data.options)
        {
            var button = Instantiate(optionButtonTemplate, optionRow);
            button.gameObject.SetActive(true);
            button.GetComponent<Image>().color = option.kind == OptionKind.Combat ? combatColor
                : option.kind == OptionKind.Loot ? lootColor : socialColor;
            button.transform.Find("Name").GetComponent<Text>().text = option.buttonName;
            button.transform.Find("Kind").GetComponent<Text>().text = option.kind.ToString();
            var picked = option;
            button.onClick.AddListener(() => OnOptionPicked(picked));
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    // Combat options open the combat window, social options the social window.
    // Loot options draw an item card and finish the event.
    void OnOptionPicked(EventOption option)
    {
        if (option.kind == OptionKind.Loot)
        {
            Hide();
            ResolveCurrentEvent();
            var items = FindFirstObjectByType<ItemDeck>();
            if (items) items.Draw();   // Then click a player to give them the card.
            var story = FindFirstObjectByType<StoryCard>();
            if (story) story.AdvanceWhenEventsDone();
            return;
        }
        if (option.kind == OptionKind.Social)
        {
            var talk = FindFirstObjectByType<SocialPanel>(FindObjectsInactive.Include);
            if (!talk) return;
            Hide();
            talk.Open(option.social, option.buttonName);
            return;
        }
        if (option.kind != OptionKind.Combat) return;
        var combat = FindFirstObjectByType<CombatPanel>(FindObjectsInactive.Include);
        if (!combat) return;
        Hide();
        combat.Open(option.combat, option.buttonName);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
