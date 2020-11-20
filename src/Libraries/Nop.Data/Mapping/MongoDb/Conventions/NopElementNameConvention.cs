using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Nop.Data.Mapping.MongoDb.Conventions
{
    public class NopElementNameConvention : ConventionBase, IMemberMapConvention
    {
        // public methods
        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            memberMap.SetElementName(NameCompatibilityManager.GetColumnName(memberMap.ClassMap.ClassType, memberMap.MemberName));
        }
    }
}