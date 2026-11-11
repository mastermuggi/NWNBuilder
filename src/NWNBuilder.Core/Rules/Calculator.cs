using NWNBuilder.Core.Models;

namespace NWNBuilder.Core.Rules;

/// <summary>Beregnede værdier efter et level.</summary>
public sealed class LevelResult
{
    public int Level { get; init; }
    public string Class { get; init; } = "";
    public int ClassLevel { get; init; }
    public AbilityScores Abilities { get; init; } = new();
    public int Bab { get; init; }
    public int Fort { get; init; }
    public int Ref { get; init; }
    public int Will { get; init; }
    public int Hp { get; init; }
    public int SkillPoints { get; init; }
    public int SkillPointsTotal { get; init; }
    public int MaxRanks { get; init; }
}

public sealed class BuildResult
{
    /// <summary>Start-stats + race + hard gifts.</summary>
    public AbilityScores StartAbilities { get; init; } = new();
    public List<LevelResult> Levels { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public LevelResult? Final => Levels.LastOrDefault();
}

public static class Calculator
{
    public static int Mod(int score) => (int)Math.Floor((score - 10) / 2.0);

    public static BuildResult Calculate(Build build, Ruleset rules)
    {
        var warnings = new List<string>();
        var race = rules.FindRace(build.Race);
        if (race == null && !string.IsNullOrWhiteSpace(build.Race))
            warnings.Add($"Race '{build.Race}' findes ikke i regelsættet.");

        var gifts = new List<GiftRule>();
        foreach (var name in build.GiftNames)
        {
            var gift = rules.FindGift(name);
            if (gift == null) warnings.Add($"Gift '{name}' findes ikke i regelsættet.");
            else gifts.Add(gift);
        }

        var abilities = Copy(build.StartAbilities);
        if (race != null) Add(abilities, race.Abilities);
        foreach (var g in gifts) Add(abilities, g.Abilities);
        var start = Copy(abilities);

        var classLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var preEpicLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unknownClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int featFort = 0, featRef = 0, featWill = 0, hitDice = 0, skillTotal = 0;
        var results = new List<LevelResult>();

        foreach (var lvl in build.Levels.OrderBy(l => l.Level))
        {
            if (string.IsNullOrWhiteSpace(lvl.Class))
                break; // progressionen stopper ved første tomme level

            var L = lvl.Level;
            if (lvl.AbilityIncrease is { } inc) Add(abilities, inc, 1);

            foreach (var featName in new[] { lvl.ClassFeat, lvl.GeneralFeat })
            {
                if (string.IsNullOrWhiteSpace(featName)) continue;
                var feat = rules.FindFeat(featName);
                if (feat == null) continue;
                Add(abilities, feat.Abilities);
                featFort += feat.Fort; featRef += feat.Ref; featWill += feat.Will;
            }

            var className = lvl.Class.Trim();
            classLevels[className] = classLevels.GetValueOrDefault(className) + 1;
            if (L < rules.Rules.EpicFromLevel)
                preEpicLevels[className] = preEpicLevels.GetValueOrDefault(className) + 1;

            var cls = rules.FindClass(className);
            if (cls == null && unknownClasses.Add(className))
                warnings.Add($"Klasse '{className}' findes ikke i regelsættet – tæller ikke med.");

            int intMod = Mod(abilities.Int);
            int skill = 0;
            if (cls != null)
            {
                hitDice += cls.HitDie;
                skill = Math.Max(1, cls.SkillPoints + intMod + (race?.BonusSkillPoints ?? 0));
                if (L == 1) skill *= rules.Rules.Level1SkillMultiplier;
            }
            skillTotal += skill;

            // BAB og saves: klasse-levels før epic + epic-bonus.
            int bab = 0, fort = 0, refl = 0, will = 0;
            foreach (var (name, levels) in preEpicLevels)
            {
                var c = rules.FindClass(name);
                if (c == null) continue;
                bab += BabFor(c.Bab, levels);
                fort += SaveFor(rules, c.Fort, levels);
                refl += SaveFor(rules, c.Ref, levels);
                will += SaveFor(rules, c.Will, levels);
            }
            if (L >= rules.Rules.EpicFromLevel)
            {
                bab += (L - rules.Rules.EpicFromLevel) / 2 + 1;
                var epicSave = (L - rules.Rules.EpicFromLevel + 1) / 2;
                fort += epicSave; refl += epicSave; will += epicSave;
            }

            foreach (var (name, levels) in classLevels)
            {
                var c = rules.FindClass(name);
                if (c == null) continue;
                foreach (var b in c.SaveBonuses.Where(b => levels >= b.ClassLevel))
                {
                    fort += b.Fort; refl += b.Ref; will += b.Will;
                }
            }

            var allSaves = gifts.Sum(g => g.AllSaves);
            fort += featFort + allSaves + Mod(abilities.Con);
            refl += featRef + allSaves + Mod(abilities.Dex);
            will += featWill + allSaves + Mod(abilities.Wis);

            results.Add(new LevelResult
            {
                Level = L,
                Class = className,
                ClassLevel = classLevels[className],
                Abilities = Copy(abilities),
                Bab = bab,
                Fort = fort,
                Ref = refl,
                Will = will,
                Hp = hitDice + Mod(abilities.Con) * L, // max HP pr. level; CON gælder bagud
                SkillPoints = skill,
                SkillPointsTotal = skillTotal,
                MaxRanks = L + rules.Rules.MaxRanksAboveLevel,
            });
        }

        return new BuildResult { StartAbilities = start, Levels = results, Warnings = warnings };
    }

    private static int BabFor(string type, int levels) => type switch
    {
        "full" => levels,
        "3/4" => levels * 3 / 4,
        "1/2" => levels / 2,
        _ => 0,
    };

    private static int SaveFor(Ruleset rules, string table, int levels)
    {
        if (levels <= 0 || !rules.SaveTables.TryGetValue(table, out var values) || values.Count == 0)
            return 0;
        return values[Math.Min(levels, values.Count) - 1];
    }

    private static AbilityScores Copy(AbilityScores a) => new()
    {
        Str = a.Str, Dex = a.Dex, Con = a.Con, Int = a.Int, Wis = a.Wis, Cha = a.Cha,
    };

    private static void Add(AbilityScores a, Dictionary<string, int> bonuses)
    {
        foreach (var (key, value) in bonuses)
            if (Enum.TryParse<Ability>(key, true, out var ability))
                Add(a, ability, value);
    }

    private static void Add(AbilityScores a, Ability ability, int value)
    {
        switch (ability)
        {
            case Ability.Str: a.Str += value; break;
            case Ability.Dex: a.Dex += value; break;
            case Ability.Con: a.Con += value; break;
            case Ability.Int: a.Int += value; break;
            case Ability.Wis: a.Wis += value; break;
            case Ability.Cha: a.Cha += value; break;
        }
    }
}
