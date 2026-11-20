using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A piece of text worked out in both languages at the moment it is made, so it can still follow the
// language setting later (a combat log line, a question, a result). Make one with
//     L.Live(() => L.T("some.key", player.playerName, item.Name))
// The code in the brackets runs once per language, so everything in it (item names, element names, other
// LocStrings) comes out in that language. A plain string turns into a LocString that is the same in both.
public sealed class LocString
{
    readonly string zh, en;

    public LocString(string zh, string en) { this.zh = zh ?? ""; this.en = en ?? ""; }

    public static implicit operator LocString(string s) => new LocString(s, s);

    public override string ToString() => L.Current == Language.English ? en : zh;

    public bool IsEmpty => zh.Length == 0 && en.Length == 0;

    public static LocString operator +(LocString a, LocString b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return new LocString(a.zh + b.zh, a.en + b.en);
    }

    public static LocString Join(string separator, IList<LocString> parts)
    {
        LocString result = "";
        for (int i = 0; i < parts.Count; i++) result = i == 0 ? parts[i] : result + separator + parts[i];
        return result;
    }

    public static bool IsNullOrEmpty(LocString s) => s == null || s.IsEmpty;
}

// Keeps a Text showing a LocString in the current language. Set from code with LiveText.Set(text, value).
// If other code later writes the Text directly, this steps aside (that code owns the text again).
public class LiveText : MonoBehaviour
{
    Text text;
    LocString value;
    string shown;
    Language shownIn;

    public static void Set(Text t, LocString v)
    {
        if (!t) return;
        var live = t.GetComponent<LiveText>();
        if (!live) live = t.gameObject.AddComponent<LiveText>();
        live.text = t;
        live.value = v ?? "";
        live.Show();
    }

    // Adds more to what the Text shows (both languages, if it is a LiveText still in charge).
    public static void Append(Text t, LocString more)
    {
        if (!t) return;
        var live = t.GetComponent<LiveText>();
        LocString now = live && live.InCharge ? live.value : (LocString)t.text;
        Set(t, now + more);
    }

    bool InCharge => text && value != null && text.text == shown;

    void Show()
    {
        shown = value.ToString();
        shownIn = L.Current;
        text.text = shown;
    }

    void OnEnable() { L.Changed += Follow; Follow(); }
    void OnDisable() { L.Changed -= Follow; }

    void Follow()
    {
        if (InCharge && shownIn != L.Current) Show();
    }
}
