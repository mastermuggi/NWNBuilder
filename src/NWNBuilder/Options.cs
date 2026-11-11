using NWNBuilder.Core.Models;

namespace NWNBuilder;

/// <summary>Faste valg til comboboxe.</summary>
public static class Options
{
    public static IReadOnlyList<Ability?> AbilityChoices { get; } =
        new Ability?[] { null, Ability.Str, Ability.Dex, Ability.Con, Ability.Int, Ability.Wis, Ability.Cha };

    public static IReadOnlyList<string> Alignments { get; } = new[]
    {
        "Lawful Good", "Neutral Good", "Chaotic Good",
        "Lawful Neutral", "True Neutral", "Chaotic Neutral",
        "Lawful Evil", "Neutral Evil", "Chaotic Evil",
    };

    /// <summary>Modul-selektor. Kun Arelith indtil videre.</summary>
    public static IReadOnlyList<string> Rulesets { get; } = new[] { "arelith" };
}
