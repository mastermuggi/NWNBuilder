using System.Collections.ObjectModel;
using System.ComponentModel;
using NWNBuilder.Core.Models;
using NWNBuilder.Core.Rules;

namespace NWNBuilder.Core;

/// <summary>Et navn med en værdi – bruges til crafting.</summary>
public sealed class NamedValue
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

/// <summary>En gift man kan vælge til/fra.</summary>
public sealed class GiftOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; init; } = "";
    public string Effect { get; init; } = "";
    public double Ecl { get; init; }
    public bool Major { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public string Label => $"{Name}  ({(Major ? "major" : "minor")}{(Ecl > 0 ? $", ECL +{Ecl}" : "")})";

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>Et level i Levels-tabellen: redigerbare valg + beregnede værdier.</summary>
public sealed class LevelRow : INotifyPropertyChanged
{
    private readonly BuildLevel _level;
    private readonly Action _changed;
    private LevelResult? _result;

    public LevelRow(BuildLevel level, Action changed, BuildEditor editor)
    {
        _level = level;
        _changed = changed;
        Editor = editor;
    }

    /// <summary>Giver dropdowns i rækken direkte adgang til valgmulighederne.</summary>
    public BuildEditor Editor { get; }

    public int Level => _level.Level;

    // null ignoreres: en dropdown sender null når den indlæses før sin liste – det må ikke slette værdien.
    // Tom tekst ("") er det bevidste "ingen" valg.
    public string Class { get => _level.Class; set { if (value == null) return; _level.Class = value; _changed(); } }
    public string? ClassFeat { get => _level.ClassFeat ?? ""; set { if (value == null) return; _level.ClassFeat = Empty(value); _changed(); } }
    public string? GeneralFeat { get => _level.GeneralFeat ?? ""; set { if (value == null) return; _level.GeneralFeat = Empty(value); _changed(); } }
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

    private static string? Empty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

/// <summary>En skill i Skills-tabellen: point brugt (redigerbart) + beregnede ranks og total.</summary>
public sealed class SkillRow : INotifyPropertyChanged
{
    private int _points;
    private SkillResult? _result;

    internal Action? Changed { get; set; }

    public string Name { get; init; } = "";
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
    private bool _showOnlyAllocated;

    public Build Build { get; }
    public IReadOnlyList<SkillRow> Skills { get; }
    public IReadOnlyList<NamedValue> Crafting { get; }
    public ObservableCollection<Friend> Friends { get; }
    public ObservableCollection<LevelRow> Rows { get; }
    public IReadOnlyList<GiftOption> Gifts { get; }

    // --- Valgmuligheder til dropdowns (fra regelsættet + buildets egne værdier) ---
    public IReadOnlyList<string> RaceOptions { get; }
    public IReadOnlyList<string> ClassOptions { get; }
    public IReadOnlyList<string> FeatOptions { get; }

    /// <summary>Fx "Human · Lawful Evil · Mask".</summary>
    public string Summary { get; }

    /// <summary>Fx ["25 Monk", "5 Swashbuckler"].</summary>
    public IReadOnlyList<string> ClassSplit { get; }

    /// <summary>Filter i Skills-fanen: vis kun skills med point.</summary>
    public bool ShowOnlyAllocated
    {
        get => _showOnlyAllocated;
        set { _showOnlyAllocated = value; Raise(nameof(ShowOnlyAllocated)); Raise(nameof(VisibleSkills)); }
    }

    public IEnumerable<SkillRow> VisibleSkills =>
        _showOnlyAllocated ? Skills.Where(s => s.Points > 0).ToList() : Skills;

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
    public string GiftLimitText
    {
        get
        {
            var race = _ruleset?.FindRace(Build.Race);
            return race == null ? "" : $"{race.Name}: {race.MajorGifts} major + {race.MinorGifts} minor (minor kan erstatte major)";
        }
    }
    public string WarningsText => Result == null ? "" : string.Join("\n", Result.Warnings);

    public event PropertyChangedEventHandler? PropertyChanged;

    public BuildEditor(Build build, Ruleset? ruleset = null)
    {
        MoveOldFieldsToNotes(build);

        Build = build;
        _ruleset = ruleset;

        // Skills: alle skills fra regelsættet + evt. andre som buildet har.
        var skillNames = (ruleset?.Skills.Select(s => s.Name) ?? Enumerable.Empty<string>())
            .Concat(build.SkillRanks.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        Skills = skillNames
            .Select(n => new SkillRow { Name = n, Points = PointsFor(build.SkillRanks, n), Changed = Recalculate })
            .OrderBy(s => s.Name)
            .ToList();

        // Crafting: faste kategorier fra regelsættet + evt. andre som buildet har.
        Crafting = (ruleset?.Crafts ?? new())
            .Concat(build.Crafting.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(n => new NamedValue { Name = n, Value = PointsFor(build.Crafting, n) })
            .ToList();

        Friends = new(build.Friends);
        Rows = new(build.Levels.OrderBy(l => l.Level).Select(l => new LevelRow(l, Recalculate, this)));

        Gifts = (ruleset?.Gifts ?? new())
            .Select(g => new GiftOption
            {
                Name = g.Name,
                Effect = g.Effect,
                Ecl = g.Ecl,
                Major = g.Major,
                IsSelected = build.GiftNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase),
            })
            .ToList();

        RaceOptions = Options(ruleset?.Races.Select(r => r.Name), new[] { build.Race }, includeEmpty: false);
        ClassOptions = Options(ruleset?.Classes.Select(c => c.Name), build.Levels.Select(l => l.Class), includeEmpty: true);
        FeatOptions = Options(ruleset?.Feats.Select(f => f.Name),
            build.Levels.SelectMany(l => new[] { l.ClassFeat, l.GeneralFeat }), includeEmpty: true);

        Summary = string.Join(" · ", new[] { build.Race, build.Alignment, build.Deity }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        ClassSplit = build.Levels
            .Where(l => !string.IsNullOrWhiteSpace(l.Class))
            .GroupBy(l => l.Class.Trim())
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        Recalculate();
    }

    /// <summary>Tjekker gift-grænsen efter et klik. Returnerer en fejl (og fravælger giften igen) hvis grænsen er overskredet.</summary>
    public string? ToggleGift(GiftOption gift)
    {
        if (_ruleset != null && gift.IsSelected)
        {
            var selected = Gifts.Where(g => g.IsSelected).Select(g => g.Name);
            var error = Calculator.GiftLimitError(selected, _ruleset.FindRace(Build.Race), _ruleset);
            if (error != null)
            {
                gift.IsSelected = false;
                return error;
            }
        }
        Recalculate();
        return null;
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
            row.SetResult(Result.Skills.FirstOrDefault(r => string.Equals(r.Name, row.Name, StringComparison.OrdinalIgnoreCase)));

        Raise(string.Empty);
    }

    public Build Apply()
    {
        Build.SkillRanks = SkillsToDictionary();
        Build.Crafting = Crafting.Where(c => c.Value != 0).ToDictionary(c => c.Name, c => c.Value);
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

    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private Dictionary<string, int> SkillsToDictionary() =>
        Skills.Where(s => s.Points != 0).ToDictionary(s => s.Name, s => s.Points);

    private static int PointsFor(Dictionary<string, int> values, string name) =>
        values.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static IReadOnlyList<string> Options(IEnumerable<string>? fromRules, IEnumerable<string?> fromBuild, bool includeEmpty)
    {
        var list = new List<string>();
        if (includeEmpty) list.Add("");
        list.AddRange((fromRules ?? Enumerable.Empty<string>())
            .Concat(fromBuild.Where(s => !string.IsNullOrWhiteSpace(s))!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)!);
        return list;
    }

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
}
