namespace NWNBuilder.Core.Models;

public enum Ability { Str, Dex, Con, Int, Wis, Cha }

public sealed class AbilityScores
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Cha { get; set; }
}

/// <summary>Ét level i et build: det der vælges på det level.</summary>
public sealed class BuildLevel
{
    public int Level { get; set; }
    public string Class { get; set; } = "";
    public string? ClassFeat { get; set; }
    public string? GeneralFeat { get; set; }
    public Ability? AbilityIncrease { get; set; }
}

/// <summary>Et character build. Kun det der vælges/skrives – beregnede værdier gemmes ikke.</summary>
public sealed class Build
{
    public string Name { get; set; } = "";
    public string Ruleset { get; set; } = "arelith";

    public string Race { get; set; } = "";
    public string Alignment { get; set; } = "";
    public string Deity { get; set; } = "";
    public int? Age { get; set; }

    /// <summary>Start-stats før gifts.</summary>
    public AbilityScores StartAbilities { get; set; } = new();
    public string Gifts { get; set; } = "";

    public List<BuildLevel> Levels { get; set; } = new();

    /// <summary>Samlede skill ranks (skill → ranks).</summary>
    public Dictionary<string, int> SkillRanks { get; set; } = new();

    /// <summary>Crafting-point (craft → point).</summary>
    public Dictionary<string, int> Crafting { get; set; } = new();

    public string Notes { get; set; } = "";
    public List<string> Origins { get; set; } = new();
    public string Voiceset { get; set; } = "";
    public string VoiceExample { get; set; } = "";
    public string Background { get; set; } = "";
}
