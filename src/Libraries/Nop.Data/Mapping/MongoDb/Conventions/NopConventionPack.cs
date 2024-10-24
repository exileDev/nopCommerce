using MongoDB.Bson.Serialization.Conventions;

namespace Nop.Data.Mapping.MongoDb.Conventions;
public class NopConventionPack : IConventionPack
{
    private NopConventionPack()
    {
        Conventions = new List<IConvention>
            {
                new IgnoreIfNullConvention(true),
                new IgnoreEnumConvention(),
                new NopElementNameConvention()
            };
    }
    public IEnumerable<IConvention> Conventions { get; }
    /// <summary>
    /// Gets the instance.
    /// </summary>
    public static IConventionPack Instance { get; } = new NopConventionPack();
}