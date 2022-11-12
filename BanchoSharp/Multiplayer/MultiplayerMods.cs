namespace BanchoSharp.Multiplayer;

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
    Relax = 1 << 12,
    Autopilot = 1 << 13,
    SpunOut = 1 << 14
}

public static class ModsExtensions
{

    /// <summary>
    /// Returns a string with a shorter format, example: "HDDT"
    /// </summary>
    public static string ToShortString(this Mods mods, bool showFreemod = true)
    {
        var ret = "";
        
        // Just going to keep appending to this string, to get everything
        // in the "right" order. This might not be the cleanest solution, 
        // but I can't think of anything better right now.

        if ((mods & Mods.Freemod) != 0 && showFreemod)
            ret += "FM";        
        if ((mods & Mods.Relax) != 0)
            ret += "RX";
        if ((mods & Mods.Autopilot) != 0)
            ret += "AP";
        if ((mods & Mods.SpunOut) != 0)
            ret += "SO";
        if ((mods & Mods.Easy) != 0)
            ret += "EZ";
        if ((mods & Mods.NoFail) != 0)
            ret += "NF";
        if ((mods & Mods.Hidden) != 0)
            ret += "HD";
        if ((mods & Mods.HalfTime) != 0)
            ret += "HT";
        if ((mods & Mods.DoubleTime) != 0)
            ret += "DT";
        if ((mods & Mods.Nightcore) != 0)
            ret += "NC";
        if ((mods & Mods.HardRock) != 0)
            ret += "HR";
        if ((mods & Mods.SuddenDeath) != 0)
            ret += "SD";
        if ((mods & Mods.Perfect) != 0)
            ret += "PF";
        if ((mods & Mods.Flashlight) != 0)
            ret += "FL";
        
        if (!ret.Any())
            ret = "None";
        
        return ret;
    }
    
}