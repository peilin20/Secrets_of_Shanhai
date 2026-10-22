// Five Elements relationships.
// 克: 水克火, 火克金, 金克木, 木克土, 土克水.
// 生: 金生水, 水生木, 木生火, 火生土, 土生金.
public static class ElementRules
{
    // True if `source` generates (生) `target`.
    public static bool Generates(Element source, Element target)
    {
        switch (source)
        {
            case Element.Metal: return target == Element.Water;
            case Element.Water: return target == Element.Wood;
            case Element.Wood: return target == Element.Fire;
            case Element.Fire: return target == Element.Earth;
            case Element.Earth: return target == Element.Metal;
        }
        return false;
    }

    // True if `attacker` counters (克) `target`.
    public static bool Counters(Element attacker, Element target)
    {
        switch (attacker)
        {
            case Element.Water: return target == Element.Fire;
            case Element.Fire: return target == Element.Metal;
            case Element.Metal: return target == Element.Wood;
            case Element.Wood: return target == Element.Earth;
            case Element.Earth: return target == Element.Water;
        }
        return false;
    }
}
