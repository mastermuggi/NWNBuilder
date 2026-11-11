using System.Collections.ObjectModel;
using System.ComponentModel;
using NWNBuilder.Core.Models;
using NWNBuilder.Core.Rules;

namespace NWNBuilder.Core;

/// <summary>Et navn med en værdi – bruges til redigerbare lister (skills, crafting).</summary>
public sealed class NamedValue
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

/// <summary>En gift man kan vælge til/fra.</summary>
public sealed class GiftOption
{
    public string Name { get; init; } = "";
    public string Effect { get; init; } = "";
    public double Ecl { get; init; }
    public bool IsSelected { get; set; }
    public string Label => Ecl > 0 ? $"{Name}  (ECL +{Ecl})" : Name;
}

/// <summary>Et level i Levels-tabellen: redigerbare valg + beregnede værdier.</summary>
public sealed class LevelRow : INotifyPropertyChanged
{
    private readonly BuildLevel _level;
    private readonly Action _changed;
    private LevelResult? _result;

    public LevelRow(BuildLevel level, Action changed)
    {
        _level = level;
        _changed = changed;
    }

    public int Level => _level.Level;
    public string Class { get => _level.Class; set { _level.Class = value ?? ""; _changed(); } }
    public string? ClassFeat { get => _level.ClassFeat; set { _level.ClassFeat = value; _changed(); } }
    public string? GeneralFeat { get => _level.GeneralFeat; set { _level.GeneralFeat = value; _changed(); } }
    public Ability? AbilityIncrease { get => _level.AbilityIncrease; set { _level.AbilityIncrease = value; _changed(); } }

    public int? Bab => _result?.Bab;
    public int? Fort => _result?.Fort;
    public int? Ref => _result?.Ref;
    public int? Will => _result?.Will;
    public int? Hp => _result?.Hp;
    public int? SkillPoints => _result?.SkillPoints;
    public int? Str => _result?.Abilities.Str;
    public int? Dex => _result?.Abilities.Dex;
    public int? Con => _result?.Abilities.Con;
    public int? Int => _result?.Abilities.Int;
    public int? Wis => _result?.Abilities.Wis;
    public int? Cha => _result?.Abilities.Cha;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetResult(LevelResult? result)
    {
        _result = result;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

/// <summary>En skill i Skills-tabellen: point brugt (redigerbart) + beregnede ranks og total.</summary>
public sealed class SkillRow : INotifyPropertyChanged
{
    private string _name = "";
    private int _points;
    private SkillResult? _result;

    internal Action? Changed { get; set; }

    public string Name { get => _name; set { _name = value ?? ""; Changed?.Invoke(); } }
    public int Points { get => _points; set { _points = value; Changed?.Invoke(); } }

    public string ClassSkill => _result == null ? "" : _result.IsClassSkill ? "✓" : "cross";
    public int? Ranks => _result?.Ranks;
    public int? MaxRanks => _result?.MaxRanks;
    public int? Modifier => _result?.Modifier;
    public int? Total => _result?.Total;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetResult(SkillResult? result)
    {
        _result = result;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

/// <summary>Redigerbar visning af et build. Apply() skriver ændringerne tilbage til Build.</summary>
public sealed class BuildEditor : INotifyPropertyChanged
{
    private readonly Ruleset? _ruleset;

    public Build Build { get; }
    public ObservableCollection<SkillRow> Skills { get; }
    public ObservableCollection<NamedValue> Crafting { get; }
    public ObservableCollection<Friend> Friends { get; }
    public ObservableCollection<LevelRow> Rows { get; }
    public IReadOnlyList<GiftOption> Gifts { get; }

    /// <summary>Fx "Human · Lawful Evil · Mask".</summary>
    public string Summary { get; }

    /// <summary>Fx ["25 Monk", "5 Swashbuckler"].</summary>
    public IReadOnlyList<string> ClassSplit { get; }

    // --- Beregnet ---
    public BuildResult? Result { get; private set; }
    public bool HasRuleset => _ruleset != null;
    public string RulesetText => _ruleset == null
        ? "Intet regelsæt – forbind og importér rulesets/arelith.json"
        : $"Regelsæt: {_ruleset.Name} ({_ruleset.Version})";
    public AbilityScores? StartWithGifts => Result?.StartAbilities;
    public LevelResult? Final => Result?.Final;
    public int SkillPointsSpent => Skills.Sum(s => s.Points);
    public string SkillPointsText => Final == null
        ? ""
        : $"Skillpoints: {SkillPointsSpent} brugt af {Final.SkillPointsTotal} · max ranks {Final.MaxRanks} (class) / {Final.MaxRanks / 2} (cross) · cross-class koster 2 point pr. rank";
    public string WarningsText => Result == null ? "" : string.Join("\n", Result.Warnings);

    public event PropertyChangedEventHandler? PropertyChanged;

    public BuildEditor(Build build, Ruleset? ruleset = null)
    {
        MoveOldFieldsToNotes(build);

        Build = build;
        _ruleset = ruleset;
        Skills = new(build.SkillRanks.Select(kv => new SkillRow { Name = kv.Key, Points = kv.Value }));
        foreach (var row in Skills)
            row.Changed = Recalculate;
        Skills.CollectionChanged += (_, e) =>
        {
            foreach (var row in e.NewItems?.OfType<SkillRow>() ?? Enumerable.Empty<SkillRow>())
                row.Changed = Recalculate;
            Recalculate();
        };
        Crafting = new(build.Crafting.Select(kv => new NamedValue { Name = kv.Key, Value = kv.Value }));
        Friends = new(build.Friends);
        Rows = new(build.Levels.OrderBy(l => l.Level).Select(l => new LevelRow(l, Recalculate)));

        Gifts = (ruleset?.Gifts ?? new())
            .Select(g => new GiftOption
            {
                Name = g.Name,
                Effect = g.Effect,
                Ecl = g.Ecl,
                IsSelected = build.GiftNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase),
            })
            .ToList();

        Summary = string.Join(" · ", new[] { build.Race, build.Alignment, build.Deity }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        ClassSplit = build.Levels
            .Where(l => !string.IsNullOrWhiteSpace(l.Class))
            .GroupBy(l => l.Class.Trim())
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        Recalculate();
    }

    /// <summary>Beregner progressionen igen ud fra de nuværende valg.</summary>
    public void Recalculate()
    {
        if (_ruleset == null)
            return;

        if (Gifts.Count > 0)
            Build.GiftNames = Gifts.Where(g => g.IsSelected).Select(g => g.Name).ToList();
        Build.SkillRanks = SkillsToDictionary();

        Result = Calculator.Calculate(Build, _ruleset);
        var byLevel = Result.Levels.ToDictionary(r => r.Level);
        foreach (var row in Rows)
            row.SetResult(byLevel.GetValueOrDefault(row.Level));
        foreach (var row in Skills)
            row.SetResult(Result.Skills.FirstOrDefault(r => string.Equals(r.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public Build Apply()
    {
        Build.SkillRanks = SkillsToDictionary();
        Build.Crafting = ToDictionary(Crafting);
        Build.Friends = Friends
            .Where(f => !string.IsNullOrWhiteSpace(f.AccountName + f.PlayerName + f.Note))
            .ToList();
        if (Gifts.Count > 0)
            Build.GiftNames = Gifts.Where(g => g.IsSelected).Select(g => g.Name).ToList();
        return Build;
    }

    /// <summary>Tomt build med 30 levels.</summary>
    public static Build NewBuild() => new()
    {
        Name = "Nyt build",
        Levels = Enumerable.Range(1, 30).Select(i => new BuildLevel { Level = i }).ToList(),
    };

    /// <summary>Ældre builds havde origins/voiceset/replikker/gifts-tekst som egne felter – de lægges ind i noterne.</summary>
    private static void MoveOldFieldsToNotes(Build build)
    {
        var parts = new List<string>();
        if (build.Origins is { Count: > 0 })
            parts.Add("Origins:\n" + string.Join("\n", build.Origins));
        if (!string.IsNullOrWhiteSpace(build.Voiceset))
            parts.Add("Voiceset: " + build.Voiceset);
        if (!string.IsNullOrWhiteSpace(build.VoiceExample))
            parts.Add(build.VoiceExample);
        if (!string.IsNullOrWhiteSpace(build.Gifts))
            parts.Add("Gifts (gammel tekst): " + build.Gifts);

        if (parts.Count > 0)
            build.Notes = string.Join("\n\n", new[] { build.Notes }.Where(n => !string.IsNullOrWhiteSpace(n)).Concat(parts));

        build.Origins = null;
        build.Voiceset = null;
        build.VoiceExample = null;
        build.Gifts = null;
    }

    private Dictionary<string, int> SkillsToDictionary() =>
        ToDictionary(Skills.Select(s => new NamedValue { Name = s.Name, Value = s.Points }));

    private static Dictionary<string, int> ToDictionary(IEnumerable<NamedValue> values)
    {
        var result = new Dictionary<string, int>();
        foreach (var v in values)
        {
            var name = v.Name.Trim();
            if (name.Length > 0)
                result[name] = v.Value;
        }
        return result;
    }
}
