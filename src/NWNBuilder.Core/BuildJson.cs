using System.Text.Json;
using System.Text.Json.Serialization;
using NWNBuilder.Core.Models;
using NWNBuilder.Core.Rules;

namespace NWNBuilder.Core;

public static class BuildJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Build Load(Stream stream) =>
        JsonSerializer.Deserialize<Build>(stream, Options)
        ?? throw new InvalidDataException("Tomt build.");

    public static string Save(Build build) => JsonSerializer.Serialize(build, Options);

    /// <summary>Indlæser et regelsæt fra JSON (fx rulesets/arelith.json).</summary>
    public static Ruleset LoadRuleset(Stream stream)
    {
        var ruleset = JsonSerializer.Deserialize<Ruleset>(stream, Options)
            ?? throw new InvalidDataException("Tomt regelsæt.");
        if (string.IsNullOrWhiteSpace(ruleset.Id))
            throw new InvalidDataException("Regelsættet mangler 'id'.");
        return ruleset;
    }

    /// <summary>Indlæser et indbygget eksempel-build, fx "hesper-salcedo".</summary>
    public static Build LoadSample(string name)
    {
        var resource = $"NWNBuilder.Core.Samples.{name}.json";
        using var stream = typeof(BuildJson).Assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Eksempel findes ikke: {name}");
        return Load(stream);
    }
}
