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
    public ObservableCollection<Friend> Friends { get; }

    /// <summary>Fx "Human · Lawful Evil · Mask".</summary>
    public string Summary { get; }

    /// <summary>Fx ["25 Monk", "5 Swashbuckler"].</summary>
    public IReadOnlyList<string> ClassSplit { get; }

    public BuildEditor(Build build)
    {
        MoveOldFieldsToNotes(build);

        Build = build;
        Skills = new(build.SkillRanks.Select(kv => new NamedValue { Name = kv.Key, Value = kv.Value }));
        Crafting = new(build.Crafting.Select(kv => new NamedValue { Name = kv.Key, Value = kv.Value }));
        Friends = new(build.Friends);

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
        Build.Friends = Friends
            .Where(f => !string.IsNullOrWhiteSpace(f.AccountName + f.PlayerName + f.Note))
            .ToList();
        return Build;
    }

    /// <summary>Tomt build med 30 levels.</summary>
    public static Build NewBuild() => new()
    {
        Name = "Nyt build",
        Levels = Enumerable.Range(1, 30).Select(i => new BuildLevel { Level = i }).ToList(),
    };

    /// <summary>Ældre builds havde origins/voiceset/replikker som egne felter – de lægges ind i noterne.</summary>
    private static void MoveOldFieldsToNotes(Build build)
    {
        var parts = new List<string>();
        if (build.Origins is { Count: > 0 })
            parts.Add("Origins:\n" + string.Join("\n", build.Origins));
        if (!string.IsNullOrWhiteSpace(build.Voiceset))
            parts.Add("Voiceset: " + build.Voiceset);
        if (!string.IsNullOrWhiteSpace(build.VoiceExample))
            parts.Add(build.VoiceExample);

        if (parts.Count > 0)
            build.Notes = string.Join("\n\n", new[] { build.Notes }.Where(n => !string.IsNullOrWhiteSpace(n)).Concat(parts));

        build.Origins = null;
        build.Voiceset = null;
        build.VoiceExample = null;
    }

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
