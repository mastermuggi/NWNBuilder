using System.Security.Authentication;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NWNBuilder.Core.Models;

namespace NWNBuilder.Core;

/// <summary>Gemmer og henter builds i MongoDB (collection "builds").</summary>
public sealed class BuildStore
{
    public const string DefaultDatabase = "nwnbuilder";

    private readonly IMongoCollection<Build> _builds;

    static BuildStore()
    {
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(true),
            new IgnoreIfNullConvention(true),
        };
        ConventionRegistry.Register("NWNBuilder", pack, t => t.Namespace?.StartsWith("NWNBuilder") == true);

        BsonClassMap.RegisterClassMap<Build>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
        });
    }

    /// <param name="connectionString">MongoDB connection string.</param>
    /// <param name="database">Databasenavn. Null = navnet i connection string, ellers "nwnbuilder".</param>
    public BuildStore(string connectionString, string? database = null)
    {
        var url = MongoUrl.Create(connectionString);
        var settings = MongoClientSettings.FromUrl(url);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        // Windows (SChannel) fejlede TLS-håndtryk mod Atlas med 0x80090304 → lås til TLS 1.2.
        settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };

        var db = new MongoClient(settings).GetDatabase(database ?? url.DatabaseName ?? DefaultDatabase);
        _builds = db.GetCollection<Build>("builds");
    }

    public Task<List<Build>> ListAsync() =>
        _builds.Find(FilterDefinition<Build>.Empty).SortBy(b => b.Name).ToListAsync();

    /// <summary>Nyt build indsættes (og får Id); eksisterende erstattes.</summary>
    public Task SaveAsync(Build build) =>
        build.Id is null
            ? _builds.InsertOneAsync(build)
            : _builds.ReplaceOneAsync(b => b.Id == build.Id, build);
}
