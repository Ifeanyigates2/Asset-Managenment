using MongoDB.Driver;

namespace FrislEams.Web.Data;

public sealed class MongoIdGenerator(IMongoDatabase database)
{
    private readonly IMongoCollection<CounterDocument> counters = database.GetCollection<CounterDocument>("counters");

    public async Task<int> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CounterDocument>.Filter.Eq(c => c.Id, sequenceName);
        var update = Builders<CounterDocument>.Update.Inc(c => c.Seq, 1);
        var options = new FindOneAndUpdateOptions<CounterDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var doc = await counters.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return doc.Seq;
    }

    private sealed class CounterDocument
    {
        public string Id { get; set; } = string.Empty;
        public int Seq { get; set; }
    }
}
