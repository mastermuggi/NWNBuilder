using NWNBuilder.Core;

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
        var editor = new BuildEditor(BuildJson.LoadSample("hesper-salcedo"));

        editor.Skills.Add(new NamedValue { Name = "Appraise", Value = 5 });
        editor.Skills.Add(new NamedValue { Name = "  ", Value = 3 }); // tom række ignoreres
        editor.Crafting.Remove(editor.Crafting.First(c => c.Name == "Herbalism"));
        editor.OriginsText = "Athkatla – Amn\r\n\r\nDark Ravens\n";

        var build = editor.Apply();

        Assert.Equal(395, build.SkillRanks.Values.Sum());
        Assert.False(build.Crafting.ContainsKey("Herbalism"));
        Assert.Equal(new[] { "Athkatla – Amn", "Dark Ravens" }, build.Origins);
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
