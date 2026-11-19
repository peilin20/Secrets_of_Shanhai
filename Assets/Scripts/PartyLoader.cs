using System.Collections.Generic;
using UnityEngine;

// In the game scene: puts the players made in the character creation scene onto the character cards
// (in card order: Player A, B, C). Does nothing if the game scene was started on its own.
public class PartyLoader : MonoBehaviour
{
    public ElementToken tokenPrefab;

    void Start()
    {
        if (Party.players.Count == 0) return;
        var cards = new List<CharacterCard>(FindObjectsByType<CharacterCard>(FindObjectsInactive.Exclude));
        cards.Sort((a, b) => string.Compare(a.playerName, b.playerName, System.StringComparison.Ordinal));
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < cards.Count && i < Party.players.Count; i++)
        {
            var card = cards[i];
            var p = Party.players[i];
            card.playerName = p.name;
            card.species = p.species;
            if (p.portrait) card.portrait = p.portrait;
            card.primaryElement = p.primaryElement;
            card.elementChosen = true;
            card.background = p.background;
            card.characterClass = p.characterClass;
            card.items = new List<ItemData>(p.items);
            card.EnsureSlots();
            card.equippedSlot = p.equippedSlot;
            card.injuries = new List<InjuryData>(p.injuries);
            if (card.tokenArea)
            {
                foreach (Transform t in card.tokenArea) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
                foreach (var e in p.tokens) card.GainToken(e, tokenPrefab);
            }
            card.Refresh();
        }
    }
}
