using MongoDB.Driver;
using NWNBuilder.Core;
using NWNBuilder.Core.Models;

namespace NWNBuilder.Tests;

/// <summary>Kører mod rigtig MongoDB i CI (env MONGO_TEST_CONNECTION). Uden env springes testen over.</summary>
public class BuildStoreTests
{
    private static readonly string? Connection = Environment.GetEnvironmentVariable("MONGO_TEST_CONNECTION");

    [Fact]
    public async Task Save_and_load_Hesper()
    {
        if (string.IsNullOrEmpty(Connection))
            return;

        var dbName = "nwnbuilder_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var store = new BuildStore(Connection, dbName);
            var hesper = BuildJson.LoadSample("hesper-salcedo");

            await store.SaveAsync(hesper);
            Assert.NotNull(hesper.Id);

            // Gem igen = erstat, ikke dublet.
            hesper.Notes = "Pirate captain";
            await store.SaveAsync(hesper);

            var builds = await store.ListAsync();
            var loaded = Assert.Single(builds);

            Assert.Equal(hesper.Id, loaded.Id);
            Assert.Equal("Pirate captain", loaded.Notes);
            Assert.Equal(30, loaded.Levels.Count);
            Assert.Equal(Ability.Dex, loaded.Levels[7].AbilityIncrease);
            Assert.Null(loaded.Levels[1].GeneralFeat);
            Assert.Equal(30, loaded.SkillRanks["Craft Mastery"]);
            Assert.Equal(43, loaded.Crafting["Herbalism"]);
            Assert.Equal(17, loaded.StartAbilities.Dex);
            Assert.Equal(BuildJson.Save(hesper), BuildJson.Save(loaded));
        }
        finally
        {
            await new MongoClient(Connection).DropDatabaseAsync(dbName);
        }
    }
}
