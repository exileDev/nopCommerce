using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Nop.Data.Mapping.MongoDb.Conventions;
public class DecimalConvention : ConventionBase, IMemberMapConvention
{
    public DecimalConvention(BsonType representation)
    {
        Representation = representation;
    }
    /// <summary>
    /// Gets the representation.
    /// </summary>
    public BsonType Representation { get; }
    public void Apply(BsonMemberMap memberMap)
    {
        if (memberMap.MemberType == typeof(decimal))
        {
            var serializer = memberMap.GetSerializer();
            if (serializer is IRepresentationConfigurable representationConfigurableSerializer)
            {
                var reconfiguredSerializer = representationConfigurableSerializer.WithRepresentation(Representation);
                memberMap.SetSerializer(reconfiguredSerializer);
            }
        }
    }
}
