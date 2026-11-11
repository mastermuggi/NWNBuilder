using System.Text.Json;
using System.Text.Json.Serialization;
using NWNBuilder.Core.Models;

namespace NWNBuilder.Core;

public static class BuildJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Build Load(Stream stream) =>
        JsonSerializer.Deserialize<Build>(stream, Options)
        ?? throw new InvalidDataException("Tomt build.");

    public static string Save(Build build) => JsonSerializer.Serialize(build, Options);

    /// <summary>Indlæser et indbygget eksempel-build, fx "hesper-salcedo".</summary>
    public static Build LoadSample(string name)
    {
        var resource = $"NWNBuilder.Core.Samples.{name}.json";
        using var stream = typeof(BuildJson).Assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Eksempel findes ikke: {name}");
        return Load(stream);
    }
}
