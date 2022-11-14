namespace BanchoSharp.Multiplayer;

using System.Text;

// It's important that these are named the same way Bancho names them in IRC, 
// in order for them to get registered correctly.
[Flags]
public enum Mods
{
    None = 0,
    Freemod = 1 << 0,
    Easy = 1 << 1,
    NoFail = 1 << 2,
    HalfTime = 1 << 3,
    HardRock = 1 << 4,
    SuddenDeath = 1 << 5,
    Perfect = 1 << 6,
    DoubleTime = 1 << 7,
    Nightcore = 1 << 8,
    Hidden = 1 << 9,
    FadeIn = 1 << 10,
    Flashlight = 1 << 11,
    Autopilot = 1 << 12,
    Relax = 1 << 13,
    SpunOut = 1 << 14
}

public static class ModsExtensions
{

    /// <summary>
    /// Returns a string with a shorter format, example: "HDDT"
    /// </summary>
    public static string ToAbbreviatedForm(this Mods mods, bool showFreemod = true)
    {
        var ret = new StringBuilder(16);
        
        // Just going to keep appending to this string, to get everything
        // in the "right" order. This might not be the cleanest solution, 
        // but I can't think of anything better right now.

        if ((mods & Mods.Freemod) != 0 && showFreemod)
            ret.Append("FM");        
        if ((mods & Mods.Relax) != 0)
            ret.Append("RX");
        if ((mods & Mods.Autopilot) != 0)
            ret.Append("AP");
        if ((mods & Mods.SpunOut) != 0)
            ret.Append("SO");
        if ((mods & Mods.Easy) != 0)
            ret.Append("EZ");
        if ((mods & Mods.NoFail) != 0)
            ret.Append("NF");
        if ((mods & Mods.Hidden) != 0)
            ret.Append("HD");
        if ((mods & Mods.HalfTime) != 0)
            ret.Append("HT");
        if ((mods & Mods.DoubleTime) != 0)
            ret.Append("DT");
        if ((mods & Mods.Nightcore) != 0)
            ret.Append("NC");
        if ((mods & Mods.HardRock) != 0)
            ret.Append("HR");
        if ((mods & Mods.SuddenDeath) != 0)
            ret.Append("SD");
        if ((mods & Mods.Perfect) != 0)
            ret.Append("PF");
        if ((mods & Mods.Flashlight) != 0)
            ret.Append("FL");
        
        if (ret.Length == 0)
            ret.Append("None");
        
        return ret.ToString();
    }
    
}