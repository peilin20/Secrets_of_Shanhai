using System.Collections.Generic;
using UnityEngine;

public enum OptionKind
{
    Combat,
    Social,
    Loot      // e.g. open a chest: draw a random item card
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
