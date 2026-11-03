using System;
using UnityEngine;

/// <summary>The campaign order is explicit; scene file numbers are not chapter numbers.</summary>
public static class CampaignMap
{
    public const int Version = 2, Count = 15, PerRegion = 5;
    public static readonly string[] Regions = { "GREENHOUSE", "FOUNDRY", "FROZEN OBSERVATORY" };
    public static readonly string[] Names = {
        "THE GREENHOUSE", "ROOT SERVICE LOOP", "CANOPY JUNCTION", "POLLINATION CIRCUIT", "BIODOME LOCKDOWN",
        "THERMAL CORE", "SLAG RETURN", "FURNACE EXCHANGE", "CRUCIBLE TRANSIT", "REACTOR SHUTDOWN",
        "FROZEN OBSERVATORY", "ICE SPIRAL", "GLACIER BALANCE", "AURORA DESCENT", "LAST LIGHT"
    };
    public static readonly string[] Summaries = {
        "Original ducts and pressure lift", "Low tunnel / upper return", "Three branches / one wind shaft", "Two docks / westward return", "Three floors / central circuit",
        "Original counterweight and mass lifts", "Reset loop / closing stone", "Staggered lifts / stored impact", "Rail spur / optical bypass", "Service loop / reactor interlocks",
        "Original springs and balance beams", "Alternating spring switchbacks", "Unequal landings / paired balances", "Rise / brake / controlled descent", "Five sectors / one escape route"
    };
    public static readonly string[] Briefings = {
        "Learn to move, collect and eat. Get small for narrow passages; get heavy for the pressure lift. Your original greenhouse route leads to the next greenhouse sector.",
        "Enter the root-service tunnel small. Call the freight lift with a heavy body, then return left along the canopy. Link both service valves before taking the upper exit.",
        "A single wind shaft connects three gardens. Visit the low, middle and high relay in its requested shape. Each branch returns to the shaft; all three links open the eastern canopy.",
        "Ride east, brake at the center dock, and reach the pollination gantry. Follow the upper passage back west, record an echo, then reach the west-facing exit. The dock and both route links must remain active.",
        "Restore the lower irrigation line, middle ventilation line and upper canopy line. Follow the numbered route through the central atrium, then cross the final charged bridge to the foundry transfer.",
        "Heat changes the rhythm: resting adds weight quickly. Your original counterweight route introduces the foundry. Medium holds a platform still; light rises and heavy descends.",
        "Lower the service plate while heavy to raise the stone. Step off, shrink and cross before it closes. A lower return tunnel lets you reset. The upper return route leads to the slag outlet.",
        "Climb the staggered mass lifts and hold useful landing heights. Restore the furnace link, then land heavily on the capacitor. Cross light, follow the lower exchanger, and climb to the outlet.",
        "Dock the furnace carriage, disembark at the eastern spur and restore the optical router. Return through the overhead service route. Use the second transport line to reach the crucible outlet.",
        "Open the service echo route, dock the reactor carriage and route all three optical channels. Ascend the cooling shaft, cross the capacitor bridge and return through the reactor crown. Every interlock is required.",
        "Cold makes movement reduce mass faster. Your original spring and balance route introduces the observatory. Prepare your shape before each landing.",
        "Climb alternating spring shelves: right, left, then right again. Each shelf has a different landing shape. At the summit, descend the outside stair to the eastern exit.",
        "The observatory spans two chasms. Balance each bridge from its own approach; then use the low route to reach the far mass lift. Return above the second bridge to the summit exit.",
        "Rise through tiered airflow, then descend through a separate free-flight shaft. Brake at the middle port before continuing down. The low route opens the far spring tower and its final return bridge.",
        "Restore the observatory in five steps: spring approach, balance gallery, rail dock, optical circuit and wind descent. Plan food before leaving each checkpoint. All five sectors lead to the final outside door."
    };
    public static int Region(int stage) => Mathf.Clamp((stage - 1) / PerRegion, 0, 2);
    public static int Local(int stage) => (stage - 1) % PerRegion + 1;
    public static bool Original(int stage) => Local(stage) == 1;
    public static string Scene(int stage) => Original(stage) ? "Level" + (Region(stage) + 1) : "Sector" + stage.ToString("00");
    public static int StageForScene(string name)
    {
        if (name.StartsWith("Sector") && int.TryParse(name.Substring(6), out int sector)) return sector;
        if (name.StartsWith("Level") && int.TryParse(name.Substring(5), out int original) && original <= 3) return (original - 1) * PerRegion + 1;
        return 0;
    }
    public static int Music(int stage)
    {
        int[][] tracks = { new[] { 1, 4, 7, 4, 7 }, new[] { 2, 5, 8, 5, 8 }, new[] { 3, 6, 9, 10, 6 } };
        return tracks[Region(stage)][Local(stage) - 1];
    }
    public static void MigrateProgress()
    {
        if (PlayerPrefs.GetInt("Fit.CampaignVersion", 0) >= Version) return;
        int[] previous = { 0, 1, 6, 11, 2, 7, 12, 3, 8, 9, 14 };
        int oldUnlocked = Mathf.Clamp(PlayerPrefs.GetInt("Fit.Unlocked", 1), 1, 10);
        int oldResume = Mathf.Clamp(PlayerPrefs.GetInt("Fit.ResumeStage", oldUnlocked), 1, 10);
        var best = new int[11]; var completed = new int[11];
        for (int i = 1; i <= 10; i++) { best[i] = PlayerPrefs.GetInt("Fit.Best." + i, 0); completed[i] = PlayerPrefs.GetInt("Fit.Completed." + i, 0); }
        // Retain the old values for recovery, then map by chapter identity rather than number.
        for (int i = 1; i <= 10; i++) { PlayerPrefs.SetInt("Fit.Legacy.Best." + i, best[i]); PlayerPrefs.SetInt("Fit.Legacy.Completed." + i, completed[i]); }
        for (int i = 1; i <= Count; i++) { PlayerPrefs.DeleteKey("Fit.Best." + i); PlayerPrefs.DeleteKey("Fit.Completed." + i); }
        // Only the three preserved maps keep completion records; redesigned maps start fresh.
        for (int i = 1; i <= 3; i++) { PlayerPrefs.SetInt("Fit.Best." + previous[i], best[i]); PlayerPrefs.SetInt("Fit.Completed." + previous[i], completed[i]); }
        int unlocked = 1;
        for (int i = 1; i <= oldUnlocked; i++) unlocked = Mathf.Max(unlocked, previous[i]);
        PlayerPrefs.SetInt("Fit.Unlocked", unlocked);
        PlayerPrefs.SetInt("Fit.ResumeStage", previous[oldResume]);
        PlayerPrefs.SetInt("Fit.CampaignVersion", Version);
        PlayerPrefs.Save();
    }
}
