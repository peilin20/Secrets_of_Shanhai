using System.Collections.Generic;
using UnityEngine;

public enum OptionKind
{
    Combat,
    Social,
    Loot,     // e.g. open a chest: draw a random item card
    Choice,   // No contest: accept (ends the event) or refuse (back to the other options), e.g. 白泽 helping 赤鱬
    Story     // A plain decision (吃不吃): give its items, then turn to `nextEvent`, or end the event (→ next main story card)
}

[System.Serializable]
public class EventOption
{
    public string label;   // Shown in bold on the card, e.g. "武力威胁:"
    [TextArea(2, 6)] public string text;
    public OptionKind kind;
    public string buttonName;   // Shown on the option's button, e.g. "口水战"
    public CombatData combat;   // For combat options: the combat card this option leads to.
    public SocialData social;   // For social options: the social card this option leads to.

    [Header("Choice options")]
    public bool requiresSpecies;          // Only if someone on the team is this species (e.g. 白泽).
    public Species species;
    [TextArea(3, 10)] public string choiceText;    // Shown when the option is picked (falls back to `text`).
    public string acceptButton = "答应";
    public string refuseButton = "拒绝";
    [TextArea(2, 6)] public string acceptResult;   // Shown after accepting.

    [Header("Story options")]
    public List<ItemData> rewardItems = new List<ItemData>();   // The team picks who gets each one.
    public EventData nextEvent;                                 // Empty: the event ends.
}

// An event card: something that happens after a story card is revealed.
[CreateAssetMenu(fileName = "NewEvent", menuName = "Shanhai/Event")]
public class EventData : ScriptableObject
{
    public string title;
    public Sprite image;
    [TextArea(4, 15)] public string description;
    public List<EventOption> options = new List<EventOption>();
    [TextArea(2, 4)] public string nextStep;   // Shown in bold at the bottom, e.g. "下一步：..."
    public bool notFirstEvent;                 // Never drawn as the game's very first event (e.g. treasure chests).
}
