using NWNBuilder.Core;
using NWNBuilder.Core.Models;

namespace NWNBuilder.Tests;

public class BuildEditorTests
{
    [Fact]
    public void Hesper_summary_and_class_split()
    {
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"));

        Assert.Equal("Human · Lawful Evil · Mask", editor.Summary);
        Assert.Equal(new[] { "25 Monk", "5 Swashbuckler" }, editor.ClassSplit);
    }

    [Fact]
    public void Apply_writes_edits_back()
    {
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"), TestRules.Arelith());

        editor.Skills.Single(s => s.Name == "Appraise").Points = 5;
        editor.Crafting.Single(c => c.Name == "Herbalism").Value = 0;
        editor.Friends.Add(new Friend { AccountName = "acc123", PlayerName = "Mads", Note = "Crew på skibet" });
        editor.Friends.Add(new Friend()); // tom ven ignoreres

        var build = editor.Apply();

        Assert.Equal(395, build.SkillRanks.Values.Sum());
        Assert.False(build.Crafting.ContainsKey("Herbalism"));
        var friend = Assert.Single(build.Friends);
        Assert.Equal("acc123", friend.AccountName);
    }

    [Fact]
    public void All_skills_and_crafts_are_listed_and_filter_works()
    {
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"), TestRules.Arelith());

        Assert.Equal(28, editor.Skills.Count);
        Assert.Equal(6, editor.Crafting.Count);

        editor.ShowOnlyAllocated = true;
        Assert.Equal(13, editor.VisibleSkills.Count()); // Spellcraft har 0 point
    }

    [Fact]
    public void Gift_limit_is_enforced()
    {
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"), TestRules.Arelith());
        var might = editor.Gifts.Single(g => g.Name == "Gift of Might");

        might.IsSelected = true;
        var error = editor.ToggleGift(might);

        Assert.NotNull(error);
        Assert.False(might.IsSelected);
        Assert.Empty(editor.Result!.Warnings);
    }

    [Fact]
    public void Dropdowns_contain_rules_and_build_values()
    {
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"), TestRules.Arelith());

        Assert.Equal("", editor.ClassOptions[0]);
        Assert.Contains("Swashbuckler", editor.ClassOptions);
        Assert.Contains("Martial Novice", editor.FeatOptions);
        Assert.Contains("Human", editor.RaceOptions);
    }

    [Fact]
    public void Old_origins_and_voice_move_into_notes()
    {
        var old = new Build
        {
            Notes = "Pirate",
            Origins = new() { "Athkatla – Amn", "Dark Ravens" },
            Voiceset = "Human, Male, Blackguard",
            VoiceExample = "\"Oh dear\"",
        };

        var build = new BuildEditor(old).Apply();

        Assert.Equal("Pirate\n\nOrigins:\nAthkatla – Amn\nDark Ravens\n\nVoiceset: Human, Male, Blackguard\n\n\"Oh dear\"", build.Notes);
        Assert.Null(build.Origins);
        Assert.Null(build.Voiceset);
        Assert.Null(build.VoiceExample);
    }

    [Fact]
    public void New_build_has_30_empty_levels()
    {
        var build = BuildEditor.NewBuild();

        Assert.Equal(Enumerable.Range(1, 30), build.Levels.Select(l => l.Level));
        Assert.All(build.Levels, l => Assert.Equal("", l.Class));
        Assert.Null(build.Id);
    }
}
