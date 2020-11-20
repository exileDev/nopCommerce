using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data.Mapping;
using Nop.Data.Migrations;

namespace Nop.Data
{
    public class MongoDbDataProvider : INopDataProvider
    {
        public string ConfigurationName => "MongoDB";

        public int SupportedLengthOfBinaryHash => 0;

        public bool BackupSupported => false;

        private MongoUrl GetCurrentMongoUrl()
        {
            return new MongoUrl(DataSettingsManager.LoadSettings().ConnectionString);
        }
        private MongoClient GetClient() 
        {
            return new MongoClient(GetCurrentMongoUrl());
        }

        private IMongoDatabase GetDatabase()
        {
            return GetClient().GetDatabase(GetCurrentMongoUrl().DatabaseName);
        }

        private IMongoCollection<TEntity> GetCollection<TEntity>()
        {
            return GetDatabase().GetCollection<TEntity>(NameCompatibilityManager.GetTableName(typeof(TEntity)));
        }


        public void BackupDatabase(string fileName)
        {
            
        }

        public string BuildConnectionString(INopConnectionStringInfo nopConnectionString)
        {
            var builder = new MongoUrlBuilder 
            {
                DatabaseName = nopConnectionString.DatabaseName,
                Server = new MongoServerAddress(nopConnectionString.ServerName)
            };

            if(!nopConnectionString.IntegratedSecurity)
            {
                builder.Username = nopConnectionString.Username;
                builder.Password = nopConnectionString.Password;
            }

            return builder.ToString();
        }

        public void BulkDeleteEntities<TEntity>(IList<TEntity> entities) where TEntity : BaseEntity
        {
            GetCollection<TEntity>().InsertMany(entities);
        }

        public int BulkDeleteEntities<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity
        {
            var result = GetCollection<TEntity>().DeleteMany(predicate);
            return (int)result.DeletedCount;
        }

        public void BulkInsertEntities<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            GetCollection<TEntity>().InsertMany(entities);
        }

        public void CreateDatabase(string collation, int triesToConnect = 10)
        {
            var url = GetCurrentMongoUrl();
            var database = GetClient()
                .GetDatabase(url.DatabaseName);

            var command = new JsonCommand<BsonDocument>("{ping: 1}");
            var result = database.RunCommand(command);
            
            //TODO: to check
            if(!result["ok"].ToBoolean())
                throw new NopException();
        }

        public IDbConnection CreateDbConnection(string connectionString)
        {
            throw new NotImplementedException();
        }

        public string CreateForeignKeyName(string foreignTable, string foreignColumn, string primaryTable, string primaryColumn)
        {
            return $"FK_{foreignTable}_{foreignColumn}_{primaryTable}_{primaryColumn}";
        }

        public ITempDataStorage<TItem> CreateTempDataStorage<TItem>(string storageKey, IQueryable<TItem> query) where TItem : class
        {
            throw new NotImplementedException();
        }

        public bool DatabaseExists()
        {
            return true;
        }

        public void DeleteEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            GetCollection<TEntity>().DeleteOne<TEntity>(e => e.Id == entity.Id);
        }

        public IDictionary<int, string> GetFieldHashes<TEntity>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> keySelector, Expression<Func<TEntity, object>> fieldSelector) where TEntity : BaseEntity
        {
            throw new NotImplementedException();
        }

        public string GetIndexName(string targetTable, string targetColumn)
        {
            return $"IX_{targetTable}_{targetColumn}";
        }

        public IQueryable<TEntity> GetTable<TEntity>() where TEntity : BaseEntity
        {
            return GetCollection<TEntity>().AsQueryable();
        }

        public int? GetTableIdent<TEntity>() where TEntity : BaseEntity
        {
            throw new NotImplementedException();
        }

        public void InitializeDatabase()
        {
            var migrationManager = EngineContext.Current.Resolve<IMigrationManager>();
            migrationManager.ApplyUpMigrations(typeof(NopDbStartup).Assembly);
        }

        public TEntity InsertEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            GetCollection<TEntity>().InsertOne(entity);
            return entity;
        }

        public IList<T> Query<T>(string sql, params DataParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public IList<T> QueryProc<T>(string procedureName, params DataParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public void ReIndexTables()
        {
            throw new NotImplementedException();
        }

        public void RestoreDatabase(string backupFileName)
        {
            throw new NotImplementedException();
        }

        public void SetTableIdent<TEntity>(int ident) where TEntity : BaseEntity
        {
            throw new NotImplementedException();
        }

        public void Truncate<TEntity>(bool resetIdentity = false) where TEntity : BaseEntity
        {
            GetDatabase().DropCollection(NameCompatibilityManager.GetTableName(typeof(TEntity)));
        }

        public void UpdateEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
            GetCollection<TEntity>().ReplaceOne(filter, entity);
        }
    }
}