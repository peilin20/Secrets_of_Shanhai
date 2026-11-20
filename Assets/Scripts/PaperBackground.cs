using UnityEngine;
using UnityEngine.UI;

// The old-paper background of the opening text and character creation: Assets/Resources/UI/Paper.jpg.
// Replace that file with another picture of the same name to change it.
public static class PaperBackground
{
    public static void Apply(Image image)
    {
        if (!image) return;
        var paper = Resources.Load<Sprite>("UI/Paper");
        if (!paper) return;
        image.sprite = paper;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }
}
