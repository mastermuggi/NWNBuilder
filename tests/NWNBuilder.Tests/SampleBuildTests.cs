using NWNBuilder.Core;
using NWNBuilder.Core.Models;

namespace NWNBuilder.Tests;

public class SampleBuildTests
{
    private readonly Build _hesper = BuildJson.LoadSample("hesper-salcedo");

    [Fact]
    public void Hesper_has_30_levels_in_order()
    {
        Assert.Equal(Enumerable.Range(1, 30), _hesper.Levels.Select(l => l.Level));
    }

    [Fact]
    public void Hesper_is_25_monk_5_swashbuckler()
    {
        Assert.Equal(25, _hesper.Levels.Count(l => l.Class == "Monk"));
        Assert.Equal(5, _hesper.Levels.Count(l => l.Class == "Swashbuckler"));
    }

    [Fact]
    public void Hesper_ability_increases_every_4th_level()
    {
        var levels = _hesper.Levels.Where(l => l.AbilityIncrease != null).Select(l => l.Level);
        Assert.Equal(new[] { 4, 8, 12, 16, 20, 24, 28 }, levels);
    }

    [Fact]
    public void Hesper_skill_ranks_total_390()
    {
        Assert.Equal(390, _hesper.SkillRanks.Values.Sum());
    }

    [Fact]
    public void Build_survives_save_and_load()
    {
        var json = BuildJson.Save(_hesper);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var copy = BuildJson.Load(stream);

        Assert.Equal(json, BuildJson.Save(copy));
        Assert.Equal(Ability.Dex, copy.Levels[7].AbilityIncrease);
    }
}
