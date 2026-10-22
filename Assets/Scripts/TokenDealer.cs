using UnityEngine;

// Gives each character card a number of random element tokens when the game starts.
public class TokenDealer : MonoBehaviour
{
    public ElementToken tokenPrefab;
    public int tokensPerCard = 2;

    void Start()
    {
        // Make sure the card layouts have their final sizes before placing tokens.
        Canvas.ForceUpdateCanvases();

        foreach (var card in FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude))
        {
            if (!card.tokenArea) continue;
            for (int i = 0; i < tokensPerCard; i++)
            {
                var token = Instantiate(tokenPrefab, card.tokenArea);
                token.SetElement((Element)Random.Range(0, System.Enum.GetValues(typeof(Element)).Length));

                // Spread the tokens evenly across the area.
                float width = card.tokenArea.rect.width;
                float x = (i + 1) * width / (tokensPerCard + 1) - width * 0.5f;
                var rt = (RectTransform)token.transform;
                rt.anchoredPosition = token.ClampToParent(new Vector2(x, 0f));
            }
        }
    }
}
