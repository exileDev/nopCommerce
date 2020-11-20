using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Nop.Data.Migrations.FluentMigrator.Runner.MongoDB.Generators;

namespace Nop.Data.Migrations.FluentMigrator.Runner.MongoDB.Processors
{
    public class MongoDbProcessor : ProcessorBase
    {
        private readonly MongoUrl _mongoUrl;
        private readonly Lazy<IMongoDatabase> _database;

        public MongoDbProcessor(
            IMigrationGenerator generator,
            IOptionsSnapshot<ProcessorOptions> options,
            ILoggerProvider loggerProvider,
            IConnectionStringAccessor connectionStringAccessor) : base(generator, loggerProvider.CreateLogger(""), options.Value)
        {
#pragma warning disable 612
            ConnectionString = connectionStringAccessor.ConnectionString;
#pragma warning disable 612

            _mongoUrl = new MongoUrl(ConnectionString);
            _database =  new Lazy<IMongoDatabase>(() => new MongoClient(_mongoUrl).GetDatabase(_mongoUrl.DatabaseName));
        }

#pragma warning disable 672
        public override string ConnectionString { get; }
#pragma warning disable 672
        public override string DatabaseType => "MongoDB";

        public override IList<string> DatabaseTypeAliases { get; } = Enumerable.Empty<string>().ToList();

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            //TODO: BRB
            return true;
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            throw new NotImplementedException();
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            throw new NotImplementedException();
        }

        public override void Execute(string template, params object[] args)
        {
            throw new NotImplementedException();
        }

        public override bool Exists(string template, params object[] args)
        {
            throw new NotImplementedException();
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            throw new NotImplementedException();
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            throw new NotImplementedException();
        }

        public override DataSet Read(string template, params object[] args)
        {
            var command = new JsonCommand<BsonDocument>(string.Format(template, args));
            var result = _database.Value.RunCommand(command)["result"]?.AsBsonArray.Select(x => x.AsBsonDocument);

            if(result is null)
                return new DataSet();

            return ConvertCollectionToDataSet(result);

        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            //new DataTable(tableName).LoadDataRow()
            var collection = _database.Value.GetCollection<BsonDocument>(tableName).AsQueryable();

            return ConvertCollectionToDataSet(collection);
        }

        private DataSet ConvertCollectionToDataSet(IEnumerable<BsonDocument> collection)
        {
            if(collection is null)
                throw new ArgumentNullException(nameof(collection));

            var dt = new DataTable();
            foreach (var document in collection)
            {
                var newRow = new object[document.ElementCount];
                for (var i = 0; i < document.ElementCount; i++)
                {
                    var element = document.GetElement(i);
                    if(!dt.Columns.Contains(element.Name))
                    {
                        DataColumn dc = new DataColumn(element.Name);
                        dt.Columns.Add(dc);
                    }

                    newRow[i] = BsonTypeMapper.MapToDotNetValue(element.Value);
                }

                dt.BeginLoadData();
                dt.LoadDataRow(newRow, true);
                dt.EndLoadData();
            }

            var ds = new DataSet();
            ds.Merge(dt);

            return ds;
        }

        public override bool SchemaExists(string schemaName)
        {
            //TODO: BRB
            return true;
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            throw new NotImplementedException();
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            var collections = _database.Value.ListCollections(new ListCollectionsOptions 
            { 
                Filter = new BsonDocument("name", tableName) 
            });
            
            return collections.Any();
        }

        protected override void Dispose(bool isDisposing)
        {
        }

        protected override void Process(string sql)
        {
            if(string.IsNullOrEmpty(sql))
                return;

            var command = new JsonCommand<BsonDocument>(sql);
            _database.Value.RunCommand(command);
        }
    }
}