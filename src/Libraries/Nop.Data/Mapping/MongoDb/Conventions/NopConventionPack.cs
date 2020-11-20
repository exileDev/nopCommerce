using System.Collections.Generic;
using MongoDB.Bson.Serialization.Conventions;

namespace Nop.Data.Mapping.MongoDb.Conventions
{
    public class NopConventionPack : IConventionPack
    {
        private readonly IEnumerable<IConvention> _conventions;
        private static readonly IConventionPack _conventionPack = new NopConventionPack();
        
        private NopConventionPack()
        {
            _conventions = new List<IConvention>
            {
                new IgnoreIfNullConvention(true),
                new IgnoreEnumConvention(),
                new NopElementNameConvention()
            };
        }

        public IEnumerable<IConvention> Conventions => _conventions;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static IConventionPack Instance => _conventionPack;
    }
}