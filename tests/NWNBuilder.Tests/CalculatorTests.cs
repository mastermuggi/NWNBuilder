using NWNBuilder.Core;
using NWNBuilder.Core.Models;
using NWNBuilder.Core.Rules;

namespace NWNBuilder.Tests;

/// <summary>Tjekker beregningerne mod Hesper Salcedo-arket.</summary>
public class CalculatorTests
{
    private static Ruleset LoadArelith()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "rulesets", "arelith.json"));
        return BuildJson.LoadRuleset(stream);
    }

    private readonly BuildResult _hesper = Calculator.Calculate(BuildJson.LoadSample("hesper-salcedo"), LoadArelith());

    [Fact]
    public void No_warnings() => Assert.Empty(_hesper.Warnings);

    [Fact]
    public void Start_stats_include_gifts()
    {
        Assert.Equal(19, _hesper.StartAbilities.Dex);
        Assert.Equal(19, _hesper.StartAbilities.Int);
        Assert.Equal(10, _hesper.StartAbilities.Str);
    }

    [Fact]
    public void Final_abilities()
    {
        var a = _hesper.Final!.Abilities;
        Assert.Equal((10, 26, 10, 20, 8, 8), (a.Str, a.Dex, a.Con, a.Int, a.Wis, a.Cha));
    }

    [Fact]
    public void Bab_pre_epic_and_epic()
    {
        Assert.Equal(16, _hesper.Levels[19].Bab);
        Assert.Equal(21, _hesper.Final!.Bab);
    }

    [Fact]
    public void Saves_match_sheet()
    {
        var f = _hesper.Final!;
        Assert.Equal((19, 29, 14), (f.Fort, f.Ref, f.Will));
    }

    [Fact]
    public void Hit_points_are_max() => Assert.Equal(250, _hesper.Final!.Hp);

    [Fact]
    public void Skill_points_match_sheet()
    {
        Assert.Equal(44, _hesper.Levels[0].SkillPoints);
        Assert.Equal(11, _hesper.Levels[1].SkillPoints);
        Assert.Equal(12, _hesper.Levels[3].SkillPoints);
        Assert.Equal(390, _hesper.Final!.SkillPointsTotal);
        Assert.Equal(33, _hesper.Final.MaxRanks);
    }

    [Fact]
    public void Progression_stops_at_first_empty_level()
    {
        var result = Calculator.Calculate(BuildEditor.NewBuild(), LoadArelith());
        Assert.Empty(result.Levels);
    }

    [Fact]
    public void Editor_recalculates_when_a_level_changes()
    {
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"), LoadArelith());
        Assert.Equal(26, editor.Final!.Abilities.Dex);

        editor.Rows[27].AbilityIncrease = Ability.Str; // level 28: Dex → Str

        Assert.Equal(25, editor.Final!.Abilities.Dex);
        Assert.Equal(11, editor.Final.Abilities.Str);
        Assert.Equal(11, editor.Rows[27].Str);
    }
}
