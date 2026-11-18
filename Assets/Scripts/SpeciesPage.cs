using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The species step of character creation, laid out in the scene so it can be edited in the Scene window.
// The game fills in the texts (both languages) and the portraits from GameSetup's species list, in order.
public class SpeciesPage : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public Button button;       // The clickable area.
        public HoverTint tint;      // Grey until hovered / picked.
        public Image portrait;
        public Text nameText;
    }

    public Text title;              // "小明：选择种族"
    public Text subtitle;           // The short note under the title.
    public List<Entry> entries = new List<Entry>();   // Same order as GameSetup's species list.
    public Text description;        // The introduction, or the species under the mouse.
    public Button confirmButton;    // Shown once a species is picked.
    public Text confirmText;
}
