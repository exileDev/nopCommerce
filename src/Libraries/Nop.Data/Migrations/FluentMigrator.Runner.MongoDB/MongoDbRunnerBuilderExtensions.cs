using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Nop.Core;
using Nop.Data.Mapping.MongoDb;
using Nop.Data.Mapping.MongoDb.Conventions;
using Nop.Data.Migrations.FluentMigrator.Runner.MongoDB.Generators;
using Nop.Data.Migrations.FluentMigrator.Runner.MongoDB.Processors;

namespace Nop.Data.Migrations.FluentMigrator.Runner.MongoDB
{
    public static class MongoDbRunnerBuilderExtensions
    {
        public static IMigrationRunnerBuilder AddMongoDb(this IMigrationRunnerBuilder builder)
        {
            ConventionRegistry.Register("NopConventions", NopConventionPack.Instance, t => true);
            BsonSerializer.RegisterIdGenerator(typeof(int), NopIdGenerator.Instance);
            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128, new RepresentationConverter(allowOverflow: false, allowTruncation: false)));

            BsonClassMap.RegisterClassMap<BaseEntity>(cm => {
                cm.AutoMap();
                cm.MapIdMember(e => e.Id).SetIdGenerator(NopIdGenerator.Instance);
            });

            builder.Services.TryAddScoped<MongoDbQuoter>();

            builder.Services
                .AddScoped<MongoDbGenerator>()
                .AddScoped<IMigrationGenerator>(sp => sp.GetRequiredService<MongoDbGenerator>())
                .AddScoped<MongoDbProcessor>()
                .AddScoped<IMigrationProcessor>(sp => sp.GetRequiredService<MongoDbProcessor>());

            return builder;
        }
    }
}