using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Hands out the tokens a reward text promises, e.g. "每人2木token", "上场角色各获得2水token",
// "团队5金token" / "团队获得3水" / "获1土token" (team tokens: the players pick who gets each one).
// Other rewards (秘闻卡, 道具…) are still settled by the players.
public static class RewardRules
{
    static readonly Regex TokenReward = new Regex(@"(每人|上场角色各获得|上场角色各|团队获得|团队|全队|获得|获)\s*(\d+)\s*枚?\s*(金|木|水|火|土)(?!属性)");

    // Adds a line per token handed out to `lines`.
    public static IEnumerator Give(string text, List<CharacterCard> players, IReadOnlyList<CharacterCard> onStage,
                                   ElementToken tokenPrefab, List<LocString> lines)
    {
        if (string.IsNullOrEmpty(text) || !tokenPrefab) yield break;
        foreach (Match m in TokenReward.Matches(text))
        {
            string who = m.Groups[1].Value;
            int n = int.Parse(m.Groups[2].Value);
            var element = InjuryDeck.ElementOf(m.Groups[3].Value);

            if (who == "每人" || who.StartsWith("上场角色"))
            {
                var targets = who == "每人" ? new List<CharacterCard>(players) : new List<CharacterCard>(onStage);
                foreach (var p in targets)
                {
                    int got = 0;
                    for (int i = 0; i < n; i++) got += p.GainToken(element, tokenPrefab);
                    lines.Add(L.Live(() => L.T("reward.got", p.playerName, got, L.ElementName(element))));
                }
                continue;
            }

            // Team tokens: the players decide who gets each one.
            var names = new List<LocString>();
            foreach (var p in players) names.Add(p.playerName);
            var given = new Dictionary<CharacterCard, int>();
            for (int i = 0; i < n; i++)
            {
                int pick = 0;
                yield return ChoicePrompt.Ask(L.Live(() => L.T("reward.askTeam", n, L.ElementName(element), i + 1)), names, x => pick = x);
                var p = players[pick];
                given[p] = (given.TryGetValue(p, out int c) ? c : 0) + p.GainToken(element, tokenPrefab);
            }
            foreach (var pair in given) lines.Add(L.Live(() => L.T("reward.got", pair.Key.playerName, pair.Value, L.ElementName(element))));
        }
    }
}
