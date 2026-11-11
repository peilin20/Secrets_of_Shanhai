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
    public System.Collections.Generic.List<EventData> fixedEvents = new System.Collections.Generic.List<EventData>();   // If set, these cards are drawn instead of random ones.
    public StoryData nextStory;   // The main story card that comes after this one.
}
