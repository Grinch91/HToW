using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs large numbers of simulated matches and prints the results, so balance decisions
/// rest on measurements rather than intuition.
///
/// Menu: HToW → Run Balance Report.
/// CLI:  -executeMethod BalanceReport.RunFromCommandLine
/// </summary>
public static class BalanceReport
{
    private const int Matches = 4000;

    [MenuItem("HToW/Run Balance Report")]
    public static void RunFromMenu()
    {
        Debug.Log(Build());
    }

    public static void RunFromCommandLine()
    {
        Debug.Log(Build());
        EditorApplication.Exit(0);
    }

    private static string Build()
    {
        DeckData celtic = Resources.Load<DeckData>("DeckData/Celtic");
        DeckData viking = Resources.Load<DeckData>("DeckData/Viking");

        if (celtic == null || viking == null)
        {
            return "BALANCE: could not load both decks.";
        }

        StringBuilder output = new StringBuilder();
        output.AppendLine("BALANCE ==================================================");
        output.AppendLine("BALANCE decks: " + celtic.DeckName + " (" + celtic.CardCount + " cards, "
                          + celtic.TotalMoraleCost + " morale) vs " + viking.DeckName
                          + " (" + viking.CardCount + " cards, " + viking.TotalMoraleCost + " morale)");

        // Mirror matches first: the same deck on both sides should sit at 50%. Anything
        // else means the simulator or the turn order is biased, and every other number
        // below would be untrustworthy.
        output.AppendLine("BALANCE --- sanity: mirror matches (expect ~50%) ---");
        output.AppendLine("BALANCE   Celtic mirror  " + Line(RunSeries(celtic, celtic)));
        output.AppendLine("BALANCE   Viking mirror  " + Line(RunSeries(viking, viking)));

        output.AppendLine("BALANCE --- matchup ---");
        MatchSimulator.Report head = RunSeries(celtic, viking);
        output.AppendLine("BALANCE   Celtic vs Viking  " + Line(head));

        output.AppendLine("BALANCE --- difficulty (Celtic player vs Viking AI) ---");
        foreach (AiDifficulty skill in new[] { AiDifficulty.Cautious, AiDifficulty.Balanced, AiDifficulty.Ruthless })
        {
            MatchSimulator.Report r = RunSeries(celtic, viking, AiDifficulty.Balanced, skill);
            output.AppendLine("BALANCE   vs " + skill.ToString().PadRight(9) + " " + Line(r));
        }

        output.AppendLine("BALANCE --- card performance (Celtic vs Viking series) ---");
        output.AppendLine("BALANCE   " + "card".PadRight(16) + "played".PadLeft(7)
                          + "kills".PadLeft(7) + "deaths".PadLeft(8) + "  k/d");

        List<CardData> cards = new List<CardData>(CardDatabase.All);
        cards.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

        foreach (CardData card in cards)
        {
            int played = Get(head.Played, card.DisplayName);
            int kills = Get(head.Kills, card.DisplayName);
            int deaths = Get(head.Deaths, card.DisplayName);

            if (played == 0)
            {
                output.AppendLine("BALANCE   " + card.DisplayName.PadRight(16)
                                  + "0".PadLeft(7) + "  never played");
                continue;
            }

            string ratio = deaths == 0 ? "inf" : ((float)kills / deaths).ToString("F2");
            output.AppendLine("BALANCE   " + card.DisplayName.PadRight(16)
                              + played.ToString().PadLeft(7)
                              + kills.ToString().PadLeft(7)
                              + deaths.ToString().PadLeft(8)
                              + "  " + ratio);
        }

        output.AppendLine("BALANCE ==================================================");
        return output.ToString();
    }

    private static int Get(Dictionary<string, int> table, string key)
    {
        int value;
        table.TryGetValue(key, out value);
        return value;
    }

    private static string Line(MatchSimulator.Report r)
    {
        return string.Format("first-deck win {0,6:P1}  stalemates {1,5:P1}  avg turns {2,5:F1}",
            r.PlayerWinRate, (float)r.Stalemates / r.Matches, r.AverageTurns);
    }

    private static MatchSimulator.Report RunSeries(
        DeckData first,
        DeckData second,
        AiDifficulty firstSkill = AiDifficulty.Balanced,
        AiDifficulty secondSkill = AiDifficulty.Balanced)
    {
        MatchSimulator.Report report = new MatchSimulator.Report();

        for (int i = 0; i < Matches; i++)
        {
            // A fresh simulator per match, seeded from the index, so runs are repeatable
            // and a balance change can be compared against the previous numbers directly.
            MatchSimulator sim = new MatchSimulator(i, report);
            sim.Play(first.BuildCards(Side.Player), second.BuildCards(Side.AI), firstSkill, secondSkill);
        }

        return report;
    }
}
