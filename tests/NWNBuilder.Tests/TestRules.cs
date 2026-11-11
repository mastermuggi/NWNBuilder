using NWNBuilder.Core;
using NWNBuilder.Core.Rules;

namespace NWNBuilder.Tests;

internal static class TestRules
{
    public static Ruleset Arelith()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "rulesets", "arelith.json"));
        return BuildJson.LoadRuleset(stream);
    }
}
