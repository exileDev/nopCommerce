using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Nop.Data.Mapping.MongoDb.Conventions
{
    public class DecimalConvention: ConventionBase, IMemberMapConvention
    {
        private readonly BsonType _representation;

        public DecimalConvention(BsonType representation)
        {
            _representation = representation;
        }
        /// <summary>
        /// Gets the representation.
        /// </summary>
        public BsonType Representation => _representation;

        public void Apply(BsonMemberMap memberMap)
        {
            if(memberMap.MemberType == typeof(decimal))
            {
                var serializer = memberMap.GetSerializer();
                var representationConfigurableSerializer = serializer as IRepresentationConfigurable;
                if (representationConfigurableSerializer != null)
                {
                    var reconfiguredSerializer = representationConfigurableSerializer.WithRepresentation(_representation);
                    memberMap.SetSerializer(reconfiguredSerializer);
                }
            }
        }
    }
}