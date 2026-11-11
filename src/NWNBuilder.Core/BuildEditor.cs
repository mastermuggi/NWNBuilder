using System.Collections.ObjectModel;
using NWNBuilder.Core.Models;

namespace NWNBuilder.Core;

/// <summary>Et navn med en værdi – bruges til redigerbare lister (skills, crafting).</summary>
public sealed class NamedValue
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

/// <summary>Redigerbar visning af et build. Apply() skriver ændringerne tilbage til Build.</summary>
public sealed class BuildEditor
{
    public Build Build { get; }
    public ObservableCollection<NamedValue> Skills { get; }
    public ObservableCollection<NamedValue> Crafting { get; }

    /// <summary>Origins som tekst, én pr. linje.</summary>
    public string OriginsText { get; set; }

    /// <summary>Fx "Human · Lawful Evil · Mask".</summary>
    public string Summary { get; }

    /// <summary>Fx ["25 Monk", "5 Swashbuckler"].</summary>
    public IReadOnlyList<string> ClassSplit { get; }

    public BuildEditor(Build build)
    {
        Build = build;
        Skills = new(build.SkillRanks.Select(kv => new NamedValue { Name = kv.Key, Value = kv.Value }));
        Crafting = new(build.Crafting.Select(kv => new NamedValue { Name = kv.Key, Value = kv.Value }));
        OriginsText = string.Join(Environment.NewLine, build.Origins);

        Summary = string.Join(" · ", new[] { build.Race, build.Alignment, build.Deity }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        ClassSplit = build.Levels
            .Where(l => !string.IsNullOrWhiteSpace(l.Class))
            .GroupBy(l => l.Class.Trim())
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();
    }

    public Build Apply()
    {
        Build.SkillRanks = ToDictionary(Skills);
        Build.Crafting = ToDictionary(Crafting);
        Build.Origins = OriginsText
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        return Build;
    }

    /// <summary>Tomt build med 30 levels.</summary>
    public static Build NewBuild() => new()
    {
        Name = "Nyt build",
        Levels = Enumerable.Range(1, 30).Select(i => new BuildLevel { Level = i }).ToList(),
    };

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
