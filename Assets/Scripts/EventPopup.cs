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
                : option.kind == OptionKind.Loot || option.kind == OptionKind.Choice || option.kind == OptionKind.Story ? lootColor : socialColor;
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
        if (option.kind == OptionKind.Choice)
        {
            StartCoroutine(PickChoice(option));
            return;
        }
        if (option.kind == OptionKind.Story)
        {
            StartCoroutine(PickStory(option));
            return;
        }
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

    // A choice with no contest (e.g. 白泽 deciding whether to help 赤鱬): accepting ends the event,
    // refusing goes back to the event's other options.
    System.Collections.IEnumerator PickChoice(EventOption option)
    {
        if (option.requiresSpecies)
        {
            bool found = false;
            foreach (var p in FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude))
                if (p.species == option.species) found = true;
            if (!found)
            {
                yield return ChoicePrompt.Ask("队里没有" + CombatPanel.SpeciesName(option.species) + "，不能选这一项",
                    new System.Collections.Generic.List<string> { "确定" }, null);
                yield break;
            }
        }
        int answer = 1;
        yield return ChoicePrompt.Ask(string.IsNullOrEmpty(option.choiceText) ? option.text : option.choiceText,new System.Collections.Generic.List<string> { option.acceptButton, option.refuseButton }, x => answer = x);
        if (answer != 0) yield break;   // Refused: back to the event card.
        if (!string.IsNullOrEmpty(option.acceptResult))
            yield return ChoicePrompt.Ask(option.acceptResult, new System.Collections.Generic.List<string> { "确定" }, null);
        Hide();
        ResolveCurrentEvent();
        var story = FindFirstObjectByType<StoryCard>();
        if (story) story.AdvanceWhenEventsDone();
    }

    // A plain decision: the team hands out its items, then the card turns into the next event
    // (same slot), or the event ends and the story moves on.
    System.Collections.IEnumerator PickStory(EventOption option)
    {
        var players = new System.Collections.Generic.List<CharacterCard>(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
        players.Sort((a, b) => string.Compare(a.playerName, b.playerName, System.StringComparison.Ordinal));
        foreach (var item in option.rewardItems)
        {
            if (!item) continue;
            var takers = players.FindAll(p => p.HasRoomFor(item) && p.CanHold(item));
            if (takers.Count == 0)
            {
                yield return ChoicePrompt.Ask("没有人的背包放得下" + item.itemName + "，只能放弃", new System.Collections.Generic.List<string> { "确定" }, null);
                continue;
            }
            var names = new System.Collections.Generic.List<string>();
            foreach (var p in takers) names.Add(p.playerName);
            int pick = 0;
            yield return ChoicePrompt.Ask("获得" + item.itemName + "：给谁？", names, x => pick = x);
            takers[pick].TryAddItem(item);
            takers[pick].Refresh();
        }
        if (option.nextEvent && source)
        {
            source.SetEvent(option.nextEvent);
            Show(source);
            yield break;
        }
        Hide();
        ResolveCurrentEvent();
        var story = FindFirstObjectByType<StoryCard>();
        if (story) story.AdvanceWhenEventsDone();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
