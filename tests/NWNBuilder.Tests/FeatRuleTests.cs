using NWNBuilder.Core;
using NWNBuilder.Core.Models;
using NWNBuilder.Core.Rules;

namespace NWNBuilder.Tests;

/// <summary>Feat-krav (vanilla fra NWN Wiki + Arelith-ændringer) tjekkes pr. level.</summary>
public class FeatRuleTests
{
    private static readonly Ruleset Rules = TestRules.Arelith();

    private static Build Fighter(int levels, int str = 16, int dex = 14, int @int = 13, Action<Build>? setup = null)
    {
        var build = new Build
        {
            Race = "Human",
            StartAbilities = new AbilityScores { Str = str, Dex = dex, Con = 14, Int = @int, Wis = 10, Cha = 8 },
            Levels = Enumerable.Range(1, levels).Select(i => new BuildLevel { Level = i, Class = "Fighter" }).ToList(),
        };
        setup?.Invoke(build);
        return build;
    }

    private static List<string> Problems(Build build, int level) =>
        Calculator.Calculate(build, Rules).Levels.Single(l => l.Level == level).FeatProblems;

    [Fact]
    public void Epic_feat_before_level_21_is_flagged()
    {
        var build = Fighter(21, setup: b =>
        {
            b.Levels[5].GeneralFeat = "Epic Prowess";
            b.Levels[20].GeneralFeat = "Great Strength";
        });

        Assert.Contains(Problems(build, 6), p => p.StartsWith("Epic Prowess") && p.Contains("level 21"));
        Assert.Empty(Problems(build, 21));
    }

    [Fact]
    public void Pre_epic_rows_hide_epic_feats_in_dropdown()
    {
        var editor = new BuildEditor(Fighter(21), Rules);

        Assert.DoesNotContain("Epic Prowess", editor.Rows[5].FeatOptions);
        Assert.Contains("Epic Prowess", editor.Rows[20].FeatOptions);
        Assert.Contains("Power Attack", editor.Rows[0].FeatOptions);
    }

    [Fact]
    public void Missing_prerequisite_feats_bab_and_stats_are_listed()
    {
        var build = Fighter(4, str: 12, setup: b => b.Levels[0].GeneralFeat = "Great Cleave");

        var problem = Assert.Single(Problems(build, 1));
        Assert.Contains("BAB 4", problem);
        Assert.Contains("STR 13", problem);
        Assert.Contains("Power Attack", problem);
        Assert.Contains("Cleave", problem);
    }

    [Fact]
    public void Prerequisites_met_gives_no_problem()
    {
        var build = Fighter(6, setup: b =>
        {
            b.Levels[0].GeneralFeat = "Power Attack";
            b.Levels[0].ClassFeat = "Cleave";
            b.Levels[3].ClassFeat = "Great Cleave";
        });

        var result = Calculator.Calculate(build, Rules);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Variant_feats_count_for_their_base()
    {
        var build = Fighter(21, setup: b =>
        {
            b.Levels[0].GeneralFeat = "Weapon Focus Missile";
            b.Levels[20].GeneralFeat = "Epic Weapon Focus Missile";
        });

        Assert.Empty(Problems(build, 21));
    }

    [Fact]
    public void Arelith_lowers_ambidexterity_to_dex_13()
    {
        Assert.Empty(Problems(Fighter(1, dex: 13, setup: b => b.Levels[0].GeneralFeat = "Ambidexterity"), 1));
        Assert.NotEmpty(Problems(Fighter(1, dex: 12, setup: b => b.Levels[0].GeneralFeat = "Ambidexterity"), 1));
    }

    [Fact]
    public void Class_granted_feats_are_not_checked_and_count_as_taken()
    {
        // Monk får Stunning Fist på level 1 (BAB 8 kræves ellers) og Improved Unarmed Strike.
        var build = new Build
        {
            Race = "Human",
            StartAbilities = new AbilityScores { Str = 10, Dex = 16, Con = 14, Int = 10, Wis = 14, Cha = 8 },
            Levels = new()
            {
                new BuildLevel { Level = 1, Class = "Monk", ClassFeat = "Stunning Fist", GeneralFeat = "Deflect Arrows" },
            },
        };

        Assert.Empty(Problems(build, 1));
    }

    [Fact]
    public void Disabled_feat_is_flagged()
    {
        var build = Fighter(21, str: 26, setup: b => b.Levels[20].GeneralFeat = "Devastating Critical");

        Assert.Contains(Problems(build, 21), p => p.Contains("Arelith"));
    }

    [Fact]
    public void Feat_cells_keep_text_in_parentheses_together()
    {
        Assert.Equal(new[] { "Path:Archer (Point Blank Shot, Rapid Shot)", "Dodge" },
            Calculator.SplitFeats("Path:Archer (Point Blank Shot, Rapid Shot), Dodge"));
        Assert.Equal("Weapon Focus", Calculator.FindFeat(Rules, "Weapon Focus (Kama)")!.Name);
        Assert.Equal("Discipline: Martial (Novice)", Calculator.FindFeat(Rules, "Discipline: Martial (Novice)")!.Name);
    }

    [Fact]
    public void Feat_info_shows_description_and_requirements()
    {
        var editor = new BuildEditor(Fighter(4, str: 12, setup: b => b.Levels[0].GeneralFeat = "Great Cleave"), Rules);

        var info = editor.Rows[0].FeatInfo;
        Assert.Contains("Great Cleave", info);
        Assert.Contains("Krav:", info);
        Assert.Contains("Mangler", info);
        Assert.StartsWith("⚠", editor.Rows[0].Problems);
    }

    [Fact]
    public void Swashbuckler_gets_mobility_at_7() // wiki.nwnarelith.com/Swashbuckler
    {
        var build = new Build
        {
            Race = "Human",
            StartAbilities = new AbilityScores { Str = 10, Dex = 16, Con = 14, Int = 10, Wis = 10, Cha = 8 },
            Levels = Enumerable.Range(1, 7).Select(i => new BuildLevel { Level = i, Class = "Swashbuckler" }).ToList(),
        };
        build.Levels[6].ClassFeat = "Mobility";

        Assert.Empty(Problems(build, 7));
    }
}
