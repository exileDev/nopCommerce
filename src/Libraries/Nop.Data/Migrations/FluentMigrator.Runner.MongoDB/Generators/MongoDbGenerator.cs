using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.Base;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data.Mapping.MongoDb;

namespace Nop.Data.Migrations.FluentMigrator.Runner.MongoDB.Generators
{
    public class MongoDbGenerator : GeneratorBase
    {
        private readonly MongoUrl _mongoUrl;
        private readonly Lazy<IMongoDatabase> _database;
        
        public MongoDbGenerator(
            MongoDbQuoter quoter,
            IOptions<GeneratorOptions> generatorOptions)
            : this(new MongoDbField(new MongoDbTypeMap(), quoter), quoter, new EmptyDescriptionGenerator(), generatorOptions)
        {
            _mongoUrl = new MongoUrl(DataSettingsManager.LoadSettings().ConnectionString);
            _database =  new Lazy<IMongoDatabase>(() => new MongoClient(_mongoUrl).GetDatabase(_mongoUrl.DatabaseName));
        }

        protected MongoDbGenerator(
            IColumn column,
            IQuoter quoter,
            IDescriptionGenerator descriptionGenerator,
            IOptions<GeneratorOptions> generatorOptions) : base(column, quoter, descriptionGenerator)
        {
            
        }

        public override string Generate(CreateSchemaExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteSchemaExpression expression)
        {
            return "";
        }

        public override string Generate(CreateTableExpression expression)
        {
            var options = new CreateCollectionOptions<BsonDocument>
            {
                UsePowerOf2Sizes = true,
                ValidationAction = DocumentValidationAction.Error,
                ValidationLevel = DocumentValidationLevel.Moderate,
                Validator = new BsonDocument("x", 1)
            };

            var client = new MongoClient(_mongoUrl);
            var session = client.StartSession();


            var binding = new WritableServerBinding(client.Cluster, session.WrappedCoreSession.Fork());
            
            using(var channelSource = new ReadWriteBindingHandle(binding))
            {
                var columns = expression.Columns.Where(column => column.Name != nameof(BaseEntity.Id));

                //var validator = new BsonDocument("_id", new BsonDocument("$exists", true));
                var validator = new BsonDocument("$jsonSchema", new BsonDocument {
                    { 
                        "bsonType", "object" 
                    },
                    {
                        "required", new BsonArray(columns.Where(column => !(column.IsNullable ?? false)).Select(column => BsonValue.Create(column.Name))) 
                    },
                    { 
                        "properties", new BsonDocument(columns.Select(column =>  new BsonElement(column.Name, GetPropertyDefinition(column))))
                    }
                });

                var cn = new CollectionNamespace(_mongoUrl.DatabaseName, expression.TableName);
                var op = new CreateCollectionOperation(cn, new MessageEncoderSettings())
                {
                    Validator = validator.ToBsonDocument(),
                    ValidationLevel = DocumentValidationLevel.Strict,
                    ValidationAction = DocumentValidationAction.Error
                };

                op.Execute(channelSource, CancellationToken.None);

                var type = new Mapping.BaseNameCompatibility().TableNames.FirstOrDefault(x => x.Value == expression.TableName).Key
                    ?? new AppDomainTypeFinder().FindClassesOfType<BaseEntity>(false).Where(t => t.Name == expression.TableName).FirstOrDefault();

                var classMap = new BsonClassMap(type, BsonClassMap.LookupClassMap(typeof(BaseEntity)));
                classMap.AutoMap();
            }

            return "";
        }

        private BsonDocument GetPropertyDefinition(ColumnDefinition columnDefinition)
        {
            var fieldDefinition = new BsonDocument(); 
            
            fieldDefinition.Add(new BsonElement("bsonType", columnDefinition.Type switch {
                DbType.String => "string",
                DbType.Decimal => "decimal",
                DbType.Int32 => "int",
                DbType.Int64 => "long",
                DbType.Boolean => "bool",
                DbType.Binary => "binData",
                DbType.DateTime2 => "date",
                DbType.Guid => "binData",
                _ => throw new Exception()
            }));

            if(columnDefinition.Type == DbType.String && columnDefinition.Size.HasValue)
            {
                fieldDefinition.Add(new BsonElement("maxLength", columnDefinition.Size.Value));
            }

            return fieldDefinition;
            
        }

        public override string Generate(AlterColumnExpression expression)
        {
            return "";
        }

        public override string Generate(CreateColumnExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteTableExpression expression)
        {
            _database.Value.DropCollection(expression.TableName);
            return "";
        }

        public override string Generate(DeleteColumnExpression expression)
        {
            return "";
        }

        public override string Generate(CreateForeignKeyExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteForeignKeyExpression expression)
        {
            return "";
        }

        public override string Generate(CreateIndexExpression expression)
        {
            var collection = _database.Value.GetCollection<BsonDocument>(expression.Index.TableName);
            
            var iDefs = expression.Index.Columns.Select(c => c.Direction switch 
            { 
                Direction.Ascending => Builders<BsonDocument>.IndexKeys.Ascending(c.Name),
                Direction.Descending =>  Builders<BsonDocument>.IndexKeys.Descending(c.Name),
                _ => throw new Exception()
            });

            var res = iDefs.Aggregate((cl, cr) => Builders<BsonDocument>.IndexKeys.Combine(cl, cr));

            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(res, new CreateIndexOptions 
            { 
                Name = expression.Index.Name,
                Unique = expression.Index.IsUnique 
            }));

            return "";
        }

        public override string Generate(DeleteIndexExpression expression)
        {
            var collection = _database.Value.GetCollection<BsonDocument>(expression.Index.TableName);
            collection.Indexes.DropOne(expression.Index.Name);
            return "";
        }

        public override string Generate(RenameTableExpression expression)
        {
            _database.Value.RenameCollection(expression.OldName, expression.NewName);
            return "";
        }

        public override string Generate(RenameColumnExpression expression)
        {
            var collection = _database.Value.GetCollection<BsonDocument>(expression.TableName);

            var upd = Builders<BsonDocument>.Update.Rename(expression.OldName, expression.NewName);
            collection.UpdateOne(doc => true, upd);

            return "";
        }

        public override string Generate(InsertDataExpression expression)
        {
            var collection = _database.Value.GetCollection<BsonDocument>(expression.TableName);
            collection.InsertMany(GenerateColumnNamesAndValues(expression.Rows));

            return "";
        }

        protected IEnumerable<BsonDocument> GenerateColumnNamesAndValues(IEnumerable<List<KeyValuePair<string, object>>> rows)
        {
            foreach (var row in rows)
            {
                var newDoc = new BsonDocument();
                foreach (KeyValuePair<string, object> item in row)
                {
                    newDoc.Add(new BsonElement(item.Key, BsonValue.Create(item.Value)));
                }
                yield return newDoc;
            }
        }

        public override string Generate(AlterDefaultConstraintExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteDataExpression expression)
        {
            var collection = _database.Value.GetCollection<BsonDocument>(expression.TableName);
            
            if(expression.IsAllRows)
                collection.DeleteMany(_ => true);
            else if (expression.Rows.Any())
            {
                var filterDocuments = GenerateColumnNamesAndValues(expression.Rows).Aggregate((l, r) => l.Merge(l));
                collection.DeleteMany(filterDocuments);
            }

            return "";
        }

        public override string Generate(UpdateDataExpression expression)
        {
            return "";
        }

        public override string Generate(AlterSchemaExpression expression)
        {
            return "";
        }

        public override string Generate(CreateSequenceExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteSequenceExpression expression)
        {
            return "";
        }

        public override string Generate(CreateConstraintExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteConstraintExpression expression)
        {
            return "";
        }

        public override string Generate(DeleteDefaultConstraintExpression expression)
        {
            return "";
        }
    }
}