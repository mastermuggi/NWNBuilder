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
    /// <summary>Database-id. Null indtil buildet er gemt første gang.</summary>
    public string? Id { get; set; }

    public string Name { get; set; } = "";
    public string Ruleset { get; set; } = "arelith";

    public string Race { get; set; } = "";
    public string Alignment { get; set; } = "";
    public string Deity { get; set; } = "";
    public int? Age { get; set; }

    /// <summary>Start-stats før race og gifts.</summary>
    public AbilityScores StartAbilities { get; set; } = new();

    /// <summary>Valgte gifts (navne fra regelsættet).</summary>
    public List<string> GiftNames { get; set; } = new();

    public List<BuildLevel> Levels { get; set; } = new();

    /// <summary>Samlede skill ranks (skill → ranks).</summary>
    public Dictionary<string, int> SkillRanks { get; set; } = new();

    /// <summary>Crafting-point (craft → point).</summary>
    public Dictionary<string, int> Crafting { get; set; } = new();

    public string Notes { get; set; } = "";
    public string Background { get; set; } = "";

    /// <summary>Vennebog.</summary>
    public List<Friend> Friends { get; set; } = new();

    // Gamle felter – læses kun fra ældre builds og flyttes ind i Notes (se BuildEditor).
    public List<string>? Origins { get; set; }
    public string? Voiceset { get; set; }
    public string? VoiceExample { get; set; }
    public string? Gifts { get; set; }
}

/// <summary>En ven i vennebogen.</summary>
public sealed class Friend
{
    public string AccountName { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Note { get; set; } = "";
}
