using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Nop.Data.Mapping.MongoDb.Conventions
{
    public class IgnoreEnumConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            if(memberMap.MemberType.IsEnum)
            {
                memberMap.SetShouldSerializeMethod(o => false);                
            }
        }
    }
}