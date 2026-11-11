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
    /// <summary>Feats på dette level, hvis krav ikke er opfyldt.</summary>
    public List<string> FeatProblems { get; init; } = new();
}

public sealed class SkillResult
{
    public string Name { get; init; } = "";
    public int Points { get; init; }
    public bool IsClassSkill { get; init; }
    public int Ranks { get; init; }
    public int MaxRanks { get; init; }
    public int Modifier { get; init; }
    public int Total => Ranks + Modifier;
}

public sealed class BuildResult
{
    public List<SkillResult> Skills { get; init; } = new();
    public int SkillPointsSpent => Skills.Sum(s => s.Points);
    /// <summary>Start-stats + race + hard gifts.</summary>
    public AbilityScores StartAbilities { get; init; } = new();
    public List<LevelResult> Levels { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public LevelResult? Final => Levels.LastOrDefault();
}

public static class Calculator
{
    public static int Mod(int score) => (int)Math.Floor((score - 10) / 2.0);

    /// <summary>Null hvis gift-valget er tilladt for racen, ellers en forklaring.</summary>
    public static string? GiftLimitError(IEnumerable<string> giftNames, RaceRule? race, Ruleset rules)
    {
        if (race == null) return null;
        var gifts = giftNames.Select(rules.FindGift).Where(g => g != null).ToList();
        var majors = gifts.Count(g => g!.Major);
        if (majors > race.MajorGifts)
            return $"{race.Name} må højst have {race.MajorGifts} major gift(s).";
        if (gifts.Count > race.MajorGifts + race.MinorGifts)
            return $"{race.Name} må højst have {race.MajorGifts + race.MinorGifts} gifts ({race.MajorGifts} major + {race.MinorGifts} minor).";
        return null;
    }

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

        if (GiftLimitError(build.GiftNames, race, rules) is { } giftError)
            warnings.Add(giftError);

        var abilities = Copy(build.StartAbilities);
        if (race != null) Add(abilities, race.Abilities);
        foreach (var g in gifts) Add(abilities, g.Abilities);
        var start = Copy(abilities);

        var classLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var preEpicLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unknownClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int featFort = 0, featRef = 0, featWill = 0, hitDice = 0, skillTotal = 0, hpPerLevel = 0, hpFlat = 0;
        var results = new List<LevelResult>();
        var taken = new List<FeatRule>();
        var buildClasses = build.Levels.Select(l => rules.FindClass(l.Class)).OfType<ClassRule>().Distinct().ToList();

        foreach (var lvl in build.Levels.OrderBy(l => l.Level))
        {
            if (string.IsNullOrWhiteSpace(lvl.Class))
                break; // progressionen stopper ved første tomme level

            var L = lvl.Level;
            if (lvl.AbilityIncrease is { } inc) Add(abilities, inc, 1);

            var cls = rules.FindClass(lvl.Class);
            var className = cls?.Name ?? lvl.Class.Trim();
            classLevels[className] = classLevels.GetValueOrDefault(className) + 1;
            if (L < rules.Rules.EpicFromLevel)
                preEpicLevels[className] = preEpicLevels.GetValueOrDefault(className) + 1;

            if (cls == null && unknownClasses.Add(className))
                warnings.Add($"Klasse '{className}' findes ikke i regelsættet – tæller ikke med.");

            foreach (var bonus in cls?.AbilityBonuses.Where(b => b.ClassLevel == classLevels[className]) ?? Enumerable.Empty<AbilityBonus>())
            {
                var key = bonus.Ability.Equals("StrOrDex", StringComparison.OrdinalIgnoreCase)
                    ? (build.StartAbilities.Dex > build.StartAbilities.Str ? "Dex" : "Str")
                    : bonus.Ability;
                if (Enum.TryParse<Ability>(key, true, out var ab))
                    Add(abilities, ab, bonus.Value);
            }

            // Krav tjekkes med stats før dette levels feats (Great Strength tæller fra næste level).
            var beforeFeats = Copy(abilities);
            var levelFeats = SplitFeats(lvl.ClassFeat).Concat(SplitFeats(lvl.GeneralFeat))
                .Select(n => FindFeat(rules, n)).Where(f => f != null).Select(f => f!).ToList();

            foreach (var feat in levelFeats)
            {
                Add(abilities, feat.Abilities);
                featFort += feat.Fort; featRef += feat.Ref; featWill += feat.Will;
                hpPerLevel += feat.HpPerLevel; hpFlat += feat.Hp;
            }

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

            var allSaves = gifts.Sum(g => g.AllSaves) + (race?.AllSaves ?? 0);
            var hasChaToSaves = classLevels.Any(kv =>
                rules.FindClass(kv.Key)?.ChaToSavesFromLevel is { } from && kv.Value >= from);
            if (hasChaToSaves)
                allSaves += Math.Max(0, Mod(abilities.Cha));
            fort += featFort + allSaves + Mod(abilities.Con);
            refl += featRef + allSaves + Mod(abilities.Dex);
            will += featWill + allSaves + Mod(abilities.Wis);

            // Feats som klasse/race giver automatisk tæller som taget, og deres krav tjekkes ikke.
            var chosen = taken.Concat(levelFeats).ToList();
            var granted = classLevels
                .SelectMany(kv => rules.FindClass(kv.Key)?.GrantedFeats
                                      .Where(g => g.ClassLevel <= kv.Value
                                                  && (g.WithFeat == null || chosen.Contains(FindFeat(rules, g.WithFeat)!)))
                                      .Select(g => g.Feat)
                                  ?? Enumerable.Empty<string>())
                .Concat(race?.GrantedFeats ?? new())
                .Select(n => FindFeat(rules, n)).OfType<FeatRule>().ToHashSet();
            var ctx = new FeatContext(rules, L, bab, beforeFeats, classLevels,
                taken.Concat(levelFeats).Concat(granted).ToList(), build, buildClasses);
            var problems = new List<string>();
            // Også varianter: "Weapon Specialization Concussion" er dækket af en gratis Weapon Specialization.
            foreach (var feat in levelFeats.Where(f => !granted.Contains(f) && !(f.Base != null && granted.Any(g => Same(g.Name, f.Base)))))
            {
                var missing = MissingRequirements(feat, ctx);
                if (missing.Count > 0)
                {
                    problems.Add($"{feat.Name}: {string.Join(", ", missing)}");
                    warnings.Add($"Level {L}: {feat.Name} – {string.Join(", ", missing)}");
                }
            }
            taken.AddRange(levelFeats);

            results.Add(new LevelResult
            {
                FeatProblems = problems,
                Level = L,
                Class = className,
                ClassLevel = classLevels[className],
                Abilities = Copy(abilities),
                Bab = bab,
                Fort = fort,
                Ref = refl,
                Will = will,
                Hp = hitDice + (Mod(abilities.Con) + hpPerLevel) * L + hpFlat, // max HP; CON og Toughness gælder bagud
                SkillPoints = skill,
                SkillPointsTotal = skillTotal,
                MaxRanks = L + rules.Rules.MaxRanksAboveLevel,
            });
        }

        var skills = CalculateSkills(build, rules, results.LastOrDefault(), classLevels.Keys, gifts, warnings);
        return new BuildResult { StartAbilities = start, Levels = results, Skills = skills, Warnings = warnings };
    }

    /// <summary>
    /// Skills ud fra point brugt pr. skill. Class skill (for en af buildets klasser): 1 point pr. rank, max level+3.
    /// Cross-class: 2 point pr. rank, max (level+3)/2.
    /// </summary>
    private static List<SkillResult> CalculateSkills(Build build, Ruleset rules, LevelResult? final,
        IEnumerable<string> classNames, List<GiftRule> gifts, List<string> warnings)
    {
        var result = new List<SkillResult>();
        if (final == null)
            return result;

        var classes = classNames.Select(rules.FindClass).Where(c => c != null).ToList();
        foreach (var (name, points) in build.SkillRanks)
        {
            var rule = rules.FindSkill(name);
            if (rule == null)
                warnings.Add($"Skill '{name}' findes ikke i regelsættet.");

            var isClass = classes.Any(c => c!.ClassSkills.Any(cs => string.Equals(cs, name, StringComparison.OrdinalIgnoreCase)));
            if (!isClass && rule is { CrossTrainable: false } && points > 0)
                warnings.Add($"{name} kan ikke cross-trænes.");

            var ranks = isClass ? points : points / 2;
            var max = isClass ? final.MaxRanks : final.MaxRanks / 2;
            if (ranks > max)
                warnings.Add($"{name}: {ranks} ranks er over max {max}.");

            var modifier = 0;
            if (rule?.Ability != null && Enum.TryParse<Ability>(rule.Ability, true, out var ability))
                modifier += Mod(Get(final.Abilities, ability));
            modifier += gifts.Sum(g => g.Skills.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value);

            result.Add(new SkillResult
            {
                Name = name, Points = points, IsClassSkill = isClass, Ranks = ranks, MaxRanks = max, Modifier = modifier,
            });
        }

        var spent = result.Sum(s => s.Points);
        if (spent > final.SkillPointsTotal)
            warnings.Add($"Der er brugt {spent} skillpoints, men kun {final.SkillPointsTotal} til rådighed.");
        return result;
    }

    private static int Get(AbilityScores a, Ability ability) => ability switch
    {
        Ability.Str => a.Str, Ability.Dex => a.Dex, Ability.Con => a.Con,
        Ability.Int => a.Int, Ability.Wis => a.Wis, _ => a.Cha,
    };

    /// <summary>"Great Intelligence, Mummy Dust" → to feats. Komma i parentes deler ikke.</summary>
    public static IEnumerable<string> SplitFeats(string? cell) =>
        string.IsNullOrWhiteSpace(cell)
            ? Enumerable.Empty<string>()
            : System.Text.RegularExpressions.Regex.Split(cell, @"[,;/](?![^(]*\))")
                .Select(s => s.Trim()).Where(s => s.Length > 0);

    /// <summary>Slår et feat op; "Weapon Focus (Kama)" findes som "Weapon Focus" hvis den fulde tekst ikke findes.</summary>
    public static FeatRule? FindFeat(Ruleset rules, string name) =>
        rules.FindFeat(name) ?? rules.FindFeat(System.Text.RegularExpressions.Regex.Replace(name, @"\(.*?\)", "").Trim());

    private sealed record FeatContext(Ruleset Rules, int Level, int Bab, AbilityScores Abilities,
        Dictionary<string, int> ClassLevels, List<FeatRule> Feats, Build Build, List<ClassRule> BuildClasses);

    /// <summary>De krav featet mangler på dette level (tom liste = ok).</summary>
    private static List<string> MissingRequirements(FeatRule feat, FeatContext ctx)
    {
        var missing = new List<string>();
        if (feat.Disabled != null)
            missing.Add(feat.Disabled);
        var r = feat.Requires;
        var minLevel = Math.Max(r?.Level ?? 0, feat.Epic ? ctx.Rules.Rules.EpicFromLevel : 0);
        if (ctx.Level < minLevel)
            missing.Add($"kræver level {minLevel}");
        if (r == null)
            return missing;
        missing.AddRange(Missing(r, ctx, topLevel: true));
        if (r.AnyOf.Count > 0 && r.AnyOf.All(alt => Missing(alt, ctx, topLevel: false).Count > 0))
            missing.Add("et af: " + string.Join(" / ", r.AnyOf.Select(alt => string.Join(" + ", Describe(alt)))));
        return missing;
    }

    private static List<string> Missing(FeatRequirement r, FeatContext ctx, bool topLevel)
    {
        var missing = new List<string>();
        if (!topLevel && r.Level is { } lvl && ctx.Level < lvl) // top-niveauets level tjekkes sammen med epic
            missing.Add($"level {lvl}");
        if (r.MaxLevel is { } max && ctx.Level > max)
            missing.Add(max == 1 ? "kun på level 1" : $"højst level {max}");
        if (r.Bab is { } bab && ctx.Bab < bab)
            missing.Add($"BAB {bab} (har {ctx.Bab})");
        foreach (var (key, value) in r.Abilities)
            if (Enum.TryParse<Ability>(key, true, out var ab) && Get(ctx.Abilities, ab) < value)
                missing.Add($"{key.ToUpperInvariant()} {value} (har {Get(ctx.Abilities, ab)})");
        foreach (var name in r.Feats.Where(n => !HasFeat(ctx, n)))
            missing.Add(name);
        if (r.AnyFeats.Count > 0 && !r.AnyFeats.Any(n => HasFeat(ctx, n)))
            missing.Add(string.Join(" eller ", r.AnyFeats));
        foreach (var (name, ranks) in r.Skills)
        {
            var have = SkillRanksAt(ctx, name);
            if (have < ranks) missing.Add($"{name} {ranks} ranks (højst {have})");
        }
        foreach (var (name, levels) in r.ClassLevels)
        {
            var cls = ctx.Rules.FindClass(name)?.Name ?? name;
            if (ctx.ClassLevels.GetValueOrDefault(cls) < levels)
                missing.Add($"{cls} {levels}");
        }
        return missing;
    }

    private static IEnumerable<string> Describe(FeatRequirement r)
    {
        foreach (var (k, v) in r.ClassLevels) yield return $"{k} {v}";
        foreach (var f in r.Feats) yield return f;
        foreach (var (k, v) in r.Abilities) yield return $"{k.ToUpperInvariant()} {v}";
        if (r.Bab is { } b) yield return $"BAB {b}";
    }

    /// <summary>Har buildet featet (eller en variant af det, fx "Weapon Focus Missile" for "Weapon Focus")?</summary>
    private static bool HasFeat(FeatContext ctx, string name)
    {
        var wanted = ctx.Rules.FindFeat(name)?.Name ?? name;
        return ctx.Feats.Any(f => Same(f.Name, wanted) || (f.Base != null && Same(f.Base, wanted)));
    }

    /// <summary>Højst mulige ranks på dette level ud fra de point der er brugt i alt (skills gemmes ikke pr. level).</summary>
    private static int SkillRanksAt(FeatContext ctx, string skill)
    {
        var points = ctx.Build.SkillRanks.FirstOrDefault(kv => Same(kv.Key, skill)).Value;
        var isClass = ctx.BuildClasses.Any(c => c.ClassSkills.Any(cs => Same(cs, skill)));
        var max = ctx.Level + ctx.Rules.Rules.MaxRanksAboveLevel;
        return isClass ? Math.Min(points, max) : Math.Min(points / 2, max / 2);
    }

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

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
