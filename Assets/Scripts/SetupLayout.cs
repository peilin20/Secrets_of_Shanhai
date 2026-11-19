using UnityEngine;
using UnityEngine.UI;

// One layout of the character creation screen: the prompt, the choice buttons and the text under them.
// The scene has two: "无角色卡布局" (name and age, centred, no card) and "有角色卡布局" (element, background, class,
// with the card on the left). Move and resize them freely in the Scene window; the game only fills in the text.
public class SetupLayout : MonoBehaviour
{
    public Text stepText;           // The prompt ("输入你的名字" …).
    public RectTransform choiceRow; // The buttons go here (its Grid Layout Group sets their size and spacing).
    public Text detailText;         // The text under the buttons.
    public Text introText;          // Optional: a step's introduction above the buttons (e.g. what the main element is).
}
