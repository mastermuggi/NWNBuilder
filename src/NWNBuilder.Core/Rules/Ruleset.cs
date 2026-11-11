namespace NWNBuilder.Core.Rules;

/// <summary>Et regelsæt (fx Arelith). Indlæses fra JSON og gemmes i MongoDB (collection "rulesets").</summary>
public sealed class Ruleset
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string? Notes { get; set; }

    public GeneralRules Rules { get; set; } = new();

    /// <summary>Save-tabeller pr. navn ("high", "low", "medium" …), værdi for klasse-level 1–20.</summary>
    public Dictionary<string, List<int>> SaveTables { get; set; } = new();

    public List<ClassRule> Classes { get; set; } = new();
    public List<RaceRule> Races { get; set; } = new();
    public List<GiftRule> Gifts { get; set; } = new();
    public List<SkillRule> Skills { get; set; } = new();
    public List<FeatRule> Feats { get; set; } = new();
    /// <summary>Crafting-kategorier (fx Alchemy, Smithing).</summary>
    public List<string> Crafts { get; set; } = new();

    public ClassRule? FindClass(string name) =>
        Classes.FirstOrDefault(c => Same(c.Name, name) || c.Aliases.Any(a => Same(a, name)));
    public RaceRule? FindRace(string name) =>
        Races.FirstOrDefault(r => Same(r.Name, name) || r.Aliases.Any(a => Same(a, name)));
    public GiftRule? FindGift(string name) => Gifts.FirstOrDefault(g => Same(g.Name, name));
    public SkillRule? FindSkill(string name) => Skills.FirstOrDefault(s => Same(s.Name, name));
    public FeatRule? FindFeat(string name) =>
        Feats.FirstOrDefault(f => Same(f.Name, name) || f.Aliases.Any(a => Same(a, name)));

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}

public sealed class GeneralRules
{
    public int MaxLevel { get; set; } = 30;
    public int EpicFromLevel { get; set; } = 21;
    public int Level1SkillMultiplier { get; set; } = 4;
    public int MaxRanksAboveLevel { get; set; } = 3;
    public string? Notes { get; set; }
}

public sealed class ClassRule
{
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
    public int HitDie { get; set; }
    /// <summary>"full", "3/4" eller "1/2".</summary>
    public string Bab { get; set; } = "full";
    public string Fort { get; set; } = "low";
    public string Ref { get; set; } = "low";
    public string Will { get; set; } = "low";
    public int SkillPoints { get; set; }
    public List<string> ClassSkills { get; set; } = new();
    public List<SaveBonus> SaveBonuses { get; set; } = new();
    public List<AbilityBonus> AbilityBonuses { get; set; } = new();
    /// <summary>CHA-mod (hvis positiv) til alle saves fra dette klasse-level (Divine Grace m.fl.). Tælles kun én gang.</summary>
    public int? ChaToSavesFromLevel { get; set; }
    /// <summary>Feats klassen giver automatisk (kravene tjekkes ikke for dem).</summary>
    public List<GrantedFeat> GrantedFeats { get; set; } = new();
    public string? Notes { get; set; }
}

public sealed class GrantedFeat
{
    public int ClassLevel { get; set; }
    public string Feat { get; set; } = "";
}

/// <summary>Stat-bonus fra en klasse når klasse-level nås. Ability "StrOrDex" = STR medmindre start-DEX er højere.</summary>
public sealed class AbilityBonus
{
    public int ClassLevel { get; set; }
    public string Ability { get; set; } = "";
    public int Value { get; set; }
}

/// <summary>Save-bonus fra en klasse når klasse-level nås (fx Swashbuckler Grace).</summary>
public sealed class SaveBonus
{
    public int ClassLevel { get; set; }
    public int Fort { get; set; }
    public int Ref { get; set; }
    public int Will { get; set; }
}

public sealed class RaceRule
{
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
    public Dictionary<string, int> Abilities { get; set; } = new();
    public int BonusSkillPoints { get; set; }
    /// <summary>Antal major/minor gifts racen må vælge (ECL-tabel). En minor kan tages i stedet for en major.</summary>
    public int MajorGifts { get; set; } = 2;
    public int MinorGifts { get; set; } = 1;
    /// <summary>Bonus til alle saves (fx Lightfoot Halfling: Lucky +1).</summary>
    public int AllSaves { get; set; }
    /// <summary>Feats racen giver automatisk (fx Half-Orc: Ambidexterity).</summary>
    public List<string> GrantedFeats { get; set; } = new();
    public string? Notes { get; set; }
}

public sealed class GiftRule
{
    public string Name { get; set; } = "";
    public double Ecl { get; set; }
    public string Effect { get; set; } = "";
    public Dictionary<string, int> Abilities { get; set; } = new();
    public Dictionary<string, int> Skills { get; set; } = new();
    public int AllSaves { get; set; }
    public bool Major { get; set; }
}

public sealed class SkillRule
{
    public string Name { get; set; } = "";
    /// <summary>"Str", "Dex" … eller null hvis ukendt.</summary>
    public string? Ability { get; set; }

    /// <summary>False = kan kun tages som class skill.</summary>
    public bool CrossTrainable { get; set; } = true;
}

public sealed class FeatRule
{
    public string Name { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
    public Dictionary<string, int> Abilities { get; set; } = new();
    public int Fort { get; set; }
    public int Ref { get; set; }
    public int Will { get; set; }
    /// <summary>Ekstra HP pr. level (fx Toughness).</summary>
    public int HpPerLevel { get; set; }
    /// <summary>Fast ekstra HP (fx Epic Toughness).</summary>
    public int Hp { get; set; }
    public string? Notes { get; set; }

    /// <summary>Hvad featet gør (vanilla-beskrivelse).</summary>
    public string? Description { get; set; }
    /// <summary>Arelith-ændringer i forhold til vanilla.</summary>
    public string? Arelith { get; set; }
    /// <summary>Kravene som tekst (også dem programmet ikke kan tjekke, fx spell level).</summary>
    public string? RequiresText { get; set; }
    /// <summary>Krav der tjekkes pr. level.</summary>
    public FeatRequirement? Requires { get; set; }
    /// <summary>Epic feat: kan først tages fra level 21.</summary>
    public bool Epic { get; set; }
    /// <summary>"class", "race", "metamagic" … (tom = general).</summary>
    public string? Type { get; set; }
    /// <summary>Variant af et andet feat, fx "Weapon Focus Missile" → "Weapon Focus".</summary>
    public string? Base { get; set; }
    /// <summary>Sat hvis featet ikke kan tages på serveren.</summary>
    public string? Disabled { get; set; }
}

public sealed class FeatRequirement
{
    public int? Level { get; set; }
    public int? MaxLevel { get; set; }
    public int? Bab { get; set; }
    public Dictionary<string, int> Abilities { get; set; } = new();
    /// <summary>Alle disse feats.</summary>
    public List<string> Feats { get; set; } = new();
    /// <summary>Mindst ét af disse feats.</summary>
    public List<string> AnyFeats { get; set; } = new();
    /// <summary>Skill → ranks.</summary>
    public Dictionary<string, int> Skills { get; set; } = new();
    /// <summary>Klasse → klasse-level.</summary>
    public Dictionary<string, int> ClassLevels { get; set; } = new();
    /// <summary>Mindst ét af disse sæt krav.</summary>
    public List<FeatRequirement> AnyOf { get; set; } = new();
}
