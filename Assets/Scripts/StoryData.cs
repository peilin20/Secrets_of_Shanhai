using UnityEngine;

// A main storyline card: sets the scene and tells players how many event cards to draw.
[CreateAssetMenu(fileName = "NewStory", menuName = "Shanhai/Story")]
public class StoryData : ScriptableObject
{
    public int number;
    public string title;
    public Sprite image;
    public string subtitle;
    [TextArea(8, 30)] public string body;
    [TextArea(2, 5)] public string nextStep;
    public int eventCardsToDraw = 1;
    public int primaryTokens = 1;   // 每位玩家主属性token获取数量: each player gets this many when the card turns up.
    public System.Collections.Generic.List<EventData> fixedEvents = new System.Collections.Generic.List<EventData>();   // If set, these cards are drawn instead of random ones.
    public StoryData nextStory;   // The main story card that comes after this one.

    [Header("English")]
    public string titleEn;
    public string subtitleEn;
    [TextArea(8, 30)] public string bodyEn;
    [TextArea(2, 5)] public string nextStepEn;

    public string Title => L.Current == Language.English && !string.IsNullOrEmpty(titleEn) ? titleEn : title;
    public string Subtitle => L.Current == Language.English && !string.IsNullOrEmpty(subtitleEn) ? subtitleEn : subtitle;
    public string Body => L.Current == Language.English && !string.IsNullOrEmpty(bodyEn) ? bodyEn : body;
    public string NextStep => L.Current == Language.English && !string.IsNullOrEmpty(nextStepEn) ? nextStepEn : nextStep;
}
