using System;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Nop.Core;

namespace Nop.Data.Mapping.MongoDb
{
    public class NopIdGenerator : IIdGenerator
    {
        private static NopIdGenerator _instance = new NopIdGenerator();
        public object GenerateId(object container, object document)
        {
            var containerDynamic = (dynamic) container;
            var idSequenceCollection = containerDynamic.Database.GetCollection<dynamic>("Counters");

            var filter = Builders<dynamic>.Filter.Eq(nameof(BaseEntity.Id), containerDynamic.CollectionNamespace.CollectionName);
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
}