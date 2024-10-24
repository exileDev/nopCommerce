using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Nop.Core;

namespace Nop.Data.Mapping.MongoDb;
public class NopIdGenerator : IIdGenerator
{
    private static readonly NopIdGenerator _instance = new();
    public object GenerateId(object container, object document)
    {
        if (document is not BaseEntity entity)
            return ObjectId.GenerateNewId();

        if (container.GetType().GetProperty("Database")?.GetValue(container) is not IMongoDatabase database)
            return ObjectId.GenerateNewId();

        var idSequenceCollection = database.GetCollection<dynamic>("Counters");
        var filter = Builders<dynamic>.Filter.Eq(nameof(BaseEntity.Id), NameCompatibilityManager.GetTableName(entity.GetType()));
        var update = Builders<dynamic>.Update.Inc("Seq", 1);
        var options = new FindOneAndUpdateOptions<dynamic>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        return idSequenceCollection.FindOneAndUpdate(filter, update, options).Seq;
    }

    public bool IsEmpty(object id)
    {
        return Convert.ToInt32(id) == 0;
    }
    /// <summary>
    /// Gets an instance of NopIdGenerator.
    /// </summary>
    public static NopIdGenerator Instance
    {
        get { return _instance; }
    }
}