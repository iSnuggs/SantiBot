using LinqToDB.Common;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SantiBot.Db;

public sealed class SantiDbService : DbService
{
    private readonly IBotCredsProvider _creds;

    // these are props because creds can change at runtime
    private string DbType
        => _creds.GetCreds().Db.Type.ToLowerInvariant().Trim();

    private string ConnString
        => _creds.GetCreds().Db.ConnectionString;

    public SantiDbService(IBotCredsProvider creds)
    {
        LinqToDBForEFTools.Initialize();
        Configuration.Linq.DisableQueryCache = true;

        _creds = creds;
    }

    public override async Task SetupAsync()
    {
        await using var context = CreateRawDbContext(DbType, ConnString);

        await RunMigration(context);

        // make sure sqlite db is in wal journal mode
        if (context is SqliteContext)
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
        }
    }

    public override SantiContext CreateRawDbContext(string dbType, string connString)
    {
        switch (dbType)
        {
            case "postgresql":
            case "postgres":
            case "pgsql":
                return new PostgreSqlContext(connString);
            case "sqlite":
                return new SqliteContext(connString);
            default:
                throw new NotSupportedException($"The database provide type of '{dbType}' is not supported.");
        }
    }

    private SantiContext GetDbContextInternal()
    {
        var dbType = DbType;
        var connString = ConnString;

        var context = CreateRawDbContext(dbType, connString);
        if (context is SqliteContext)
        {
            var conn = context.Database.GetDbConnection();
            conn.Open();
            using var com = conn.CreateCommand();
            com.CommandText = "PRAGMA synchronous=OFF";
            com.ExecuteNonQuery();
        }

        return context;
    }

    public override SantiContext GetDbContext()
        => GetDbContextInternal();


    private static async Task RunMigration(DbContext ctx)
    {
        // Check if database exists and has tables
        var canConnect = await ctx.Database.CanConnectAsync();

        // Count app tables
        var tableCount = 0;
        if (canConnect)
        {
            try
            {
                var conn = ctx.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                var result = await cmd.ExecuteScalarAsync();
                tableCount = Convert.ToInt32(result);
            }
            catch { }
        }

        if (!canConnect || tableCount == 0)
        {
            Log.Information("Creating database schema from schema.sql...");
            var schemaPath = Path.Combine("data", "schema.sql");
            if (File.Exists(schemaPath))
            {
                var sql = await File.ReadAllTextAsync(schemaPath);
                var conn = ctx.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync();
                Log.Information("Schema created successfully.");
            }
            else
            {
                Log.Warning("schema.sql not found at {Path}, attempting EnsureCreated...", schemaPath);
                await ctx.Database.EnsureCreatedAsync();
            }
            return;
        }

        // get the latest applied migration
        var applied = await ctx.Database.GetAppliedMigrationsAsync();

        // get all .sql file names from the migrations folder
        var available = Directory.GetFiles("Migrations/" + GetMigrationDirectory(ctx.Database), "*_*.sql")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .OrderBy(x => x);

        if (!applied.Any())
        {
            // Existing database with tables but no migration history — skip migrations
            return;
        }

        var lastApplied = applied.Last();

        // apply all migrations with names greater than the last applied
        foreach (var runnable in available)
        {
            if (string.Compare(lastApplied, runnable, StringComparison.Ordinal) < 0)
            {
                Log.Warning("Applying migration {MigrationName}", runnable);

                var query = await File.ReadAllTextAsync(GetMigrationPath(ctx.Database, runnable));
                await ctx.Database.ExecuteSqlRawAsync(query);
            }
        }

        // run all migrations that have not been applied yet
    }

    private static string GetMigrationPath(DatabaseFacade ctxDatabase, string runnable)
    {
        return $"Migrations/{GetMigrationDirectory(ctxDatabase)}/{runnable}.sql";
    }

    private static string GetMigrationDirectory(DatabaseFacade ctxDatabase)
    {
        if (ctxDatabase.IsSqlite())
            return "Sqlite";

        if (ctxDatabase.IsNpgsql())
            return "PostgreSql";

        throw new NotSupportedException("This database type is not supported.");
    }
}