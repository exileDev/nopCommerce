using System.Linq.Expressions;
using FluentMigrator;
using LinqToDB.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data.Mapping;
using Nop.Data.Migrations;

namespace Nop.Data.DataProviders;
public partial class MongoDbDataProvider : INopDataProvider
{
    #region Fields



    #endregion

    #region Ctor

    public MongoDbDataProvider()
    {

    }

    #endregion

    #region Utilities

    private static MongoUrl GetCurrentMongoUrl()
    {
        return new MongoUrl(DataSettingsManager.LoadSettings().ConnectionString);
    }
    private static MongoClient GetClient()
    {
        var clientSettings = MongoClientSettings.FromUrl(GetCurrentMongoUrl());
        return new MongoClient(clientSettings);
    }
    private static IMongoDatabase GetDatabase()
    {
        return GetClient().GetDatabase(GetCurrentMongoUrl().DatabaseName);
    }
    private static IMongoCollection<TEntity> GetCollection<TEntity>()
    {
        return GetDatabase().GetCollection<TEntity>(NameCompatibilityManager.GetTableName(typeof(TEntity)));
    }

    #endregion

    #region Methods

    public Task BackupDatabaseAsync(string fileName)
    {
        return Task.CompletedTask;
    }

    public string BuildConnectionString(INopConnectionStringInfo nopConnectionString)
    {
        var builder = new MongoUrlBuilder
        {
            DatabaseName = nopConnectionString.DatabaseName,
            Server = new MongoServerAddress(nopConnectionString.ServerName)
        };
        if (!nopConnectionString.IntegratedSecurity)
        {
            builder.Username = nopConnectionString.Username;
            builder.Password = nopConnectionString.Password;
        }
        return builder.ToString();
    }

    public void BulkDeleteEntities<TEntity>(IList<TEntity> entities) where TEntity : BaseEntity
    {
        var filter = Builders<TEntity>.Filter
            .Where(r => entities.Any(e => e.Id == r.Id));

        GetCollection<TEntity>().DeleteMany(filter);
    }

    public int BulkDeleteEntities<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity
    {
        var result = GetCollection<TEntity>().DeleteMany(predicate);

        return (int)result.DeletedCount;
    }

    public async Task BulkDeleteEntitiesAsync<TEntity>(IList<TEntity> entities) where TEntity : BaseEntity
    {
        var filter = Builders<TEntity>.Filter
            .Where(r => entities.Any(e => e.Id == r.Id));

        await GetCollection<TEntity>().DeleteManyAsync(filter);
    }

    public async Task<int> BulkDeleteEntitiesAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity
    {
        var result = await GetCollection<TEntity>().DeleteManyAsync(predicate);
        return (int)result.DeletedCount;
    }

    public void BulkInsertEntities<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
    {
        GetCollection<TEntity>().InsertMany(entities);
    }

    public Task BulkInsertEntitiesAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
    {
        return GetCollection<TEntity>().InsertManyAsync(entities);
    }

    public void CreateDatabase(string collation, int triesToConnect = 10)
    {
        var url = GetCurrentMongoUrl();
        var database = GetClient()
            .GetDatabase(url.DatabaseName);
        var command = new JsonCommand<BsonDocument>("{ ping: 1 }");
        var result = database.RunCommand(command);
        //TODO: to check
        if (!result["ok"].ToBoolean())
            throw new NopException();
    }

    public string CreateForeignKeyName(string foreignTable, string foreignColumn, string primaryTable, string primaryColumn)
    {
        return $"FK_{foreignTable}_{foreignColumn}_{primaryTable}_{primaryColumn}";
    }

    public Task<ITempDataStorage<TItem>> CreateTempDataStorageAsync<TItem>(string storeKey, IQueryable<TItem> query) where TItem : class
    {
        throw new NotImplementedException();
    }

    public bool DatabaseExists()
    {
        return true;
    }

    public Task<bool> DatabaseExistsAsync()
    {
        return Task.FromResult(true);
    }

    public void DeleteEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        GetCollection<TEntity>().DeleteOne(e => e.Id == entity.Id);
    }

    public Task DeleteEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        return GetCollection<TEntity>().DeleteOneAsync(e => e.Id == entity.Id);
    }

    public Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] dataParameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDictionary<int, string>> GetFieldHashesAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> keySelector, Expression<Func<TEntity, object>> fieldSelector) where TEntity : BaseEntity
    {
        throw new NotImplementedException();
    }

    public string GetIndexName(string targetTable, string targetColumn)
    {
        return $"IX_{targetTable}_{targetColumn}";
    }

    public IQueryable<TEntity> GetTable<TEntity>() where TEntity : BaseEntity
    {
        var collation = new Collation("en_US", strength: CollationStrength.Secondary);
        var aggregateOptions = new AggregateOptions { Collation = collation };

        return GetCollection<TEntity>().AsQueryable(aggregateOptions);
    }

    public Task<int?> GetTableIdentAsync<TEntity>() where TEntity : BaseEntity
    {
        throw new NotImplementedException();
    }

    public void InitializeDatabase()
    {
        var migrationManager = EngineContext.Current.Resolve<IMigrationManager>();

        var targetAssembly = typeof(NopDbStartup).Assembly;
        migrationManager.ApplyUpMigrations(targetAssembly);

        var typeFinder = Singleton<ITypeFinder>.Instance;
        var mAssemblies = typeFinder.FindClassesOfType<MigrationBase>()
            .Select(t => t.Assembly)
            .Where(assembly => !assembly.FullName?.Contains("FluentMigrator.Runner") ?? false)
            .Distinct()
            .ToArray();

        //mark update migrations as applied
        foreach (var assembly in mAssemblies)
            migrationManager.ApplyUpMigrations(assembly, MigrationProcessType.Update, true);
    }

    public TEntity InsertEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        GetCollection<TEntity>().InsertOne(entity);
        return entity;
    }

    public async Task<TEntity> InsertEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        await GetCollection<TEntity>().InsertOneAsync(entity);
        return entity;
    }

    public Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IList<T>> QueryProcAsync<T>(string procedureName, params DataParameter[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task ReIndexTablesAsync()
    {
        throw new NotImplementedException();
    }

    public Task RestoreDatabaseAsync(string backupFileName)
    {
        throw new NotImplementedException();
    }

    public Task SetTableIdentAsync<TEntity>(int ident) where TEntity : BaseEntity
    {
        throw new NotImplementedException();
    }

    public Task TruncateAsync<TEntity>(bool resetIdentity = false) where TEntity : BaseEntity
    {
        return GetDatabase().DropCollectionAsync(NameCompatibilityManager.GetTableName(typeof(TEntity)));
    }

    public void UpdateEntities<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
    {
        foreach (var entity in entities)
            UpdateEntity(entity);
    }

    public async Task UpdateEntitiesAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
    {
        foreach (var entity in entities)
            await UpdateEntityAsync(entity);
    }

    public void UpdateEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
        GetCollection<TEntity>().ReplaceOne(filter, entity);
    }

    public async Task UpdateEntityAsync<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
        await GetCollection<TEntity>().ReplaceOneAsync(filter, entity);
    }

    #endregion

    #region Properties

    public string ConfigurationName => "MongoDB";

    public int SupportedLengthOfBinaryHash => 0;

    public bool BackupSupported => false;

    #endregion
}
