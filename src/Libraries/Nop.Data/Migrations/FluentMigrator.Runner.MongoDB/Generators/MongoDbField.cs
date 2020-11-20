using FluentMigrator.Model;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.Base;

namespace Nop.Data.Migrations.FluentMigrator.Runner.MongoDB.Generators
{
    public class MongoDbField : ColumnBase
    {
        public MongoDbField(ITypeMap typeMap, IQuoter quoter) : base(typeMap, quoter)
        {
        }

        protected override string FormatIdentity(ColumnDefinition column)
        {
            return string.Empty;
        }
    }
}