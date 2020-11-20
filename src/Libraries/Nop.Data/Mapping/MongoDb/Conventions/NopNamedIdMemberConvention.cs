using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Nop.Data.Mapping.MongoDb.Conventions
{
    public class NopNamedIdMemberConvention : ConventionBase, IClassMapConvention
    {
        public void Apply(BsonClassMap classMap)
        {
            throw new System.NotImplementedException();
        }
    }
}