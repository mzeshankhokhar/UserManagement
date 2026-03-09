using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Core.Model;
using System.Data;

namespace UserManagement.Repository
{
    /// <summary>
    /// Service responsible for automatic database deployment and version management.
    /// Automatically creates database and applies schema changes without manual migration commands.
    /// Preserves existing data when adding new columns.
    /// </summary>
    public static class DatabaseInitializer
    {
        private const string CurrentSchemaVersion = "1.0.1";

        /// <summary>
        /// Automatically creates/migrates the database and tracks version history.
        /// No manual 'dotnet ef migrations add' commands required.
        /// </summary>
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, bool isDevelopment = false)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<AppDbContext>>();

            try
            {
                // Check if database exists
                var canConnect = await context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    // Database doesn't exist - create it
                    logger?.LogInformation("Database does not exist. Creating database and schema...");
                    await context.Database.EnsureCreatedAsync();

                    await TryRecordVersionAsync(context, CurrentSchemaVersion, "InitialCreate",
                        "Database created automatically from model", logger);

                    logger?.LogInformation("Database created successfully.");
                    return;
                }

                // Database exists - check if it has our core tables
                var hasUsersTable = await TableExistsAsync(context, "Users");
                var hasRolesTable = await TableExistsAsync(context, "Roles");
                var hasClaimsTable = await TableExistsAsync(context, "Claims");
                var hasTokensTable = await TableExistsAsync(context, "Tokens");

                if (!hasUsersTable || !hasRolesTable || !hasClaimsTable || !hasTokensTable)
                {
                    // Some tables are missing - try to create missing tables individually
                    logger?.LogInformation("Some tables are missing. Creating missing tables...");
                    await CreateMissingTablesAsync(context, logger);
                }

                // Apply any schema updates (new columns to existing tables)
                logger?.LogInformation("Checking for schema updates...");
                await ApplySchemaUpdatesAsync(context, logger);

                // Skip migrations if tables already exist (code-first approach)
                // Migrations would fail because tables are already created
                logger?.LogInformation("Using code-first schema management (migrations skipped).");

                // Ensure version table exists
                await EnsureVersionTableExistsAsync(context, logger);

                var currentVersion = await GetCurrentVersionAsync(context);
                logger?.LogInformation("Database ready. Current version: {Version}", currentVersion);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        /// <summary>
        /// Applies schema updates (adds missing columns) without data loss
        /// </summary>
        private static async Task ApplySchemaUpdatesAsync(AppDbContext context, ILogger logger)
        {
            var schemaUpdates = new List<SchemaUpdate>
            {
                // User table updates - ensure all columns exist
                new SchemaUpdate("Users", "FirstName", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Users", "LastName", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Users", "Email", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Users", "UserName", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Users", "Password", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("Users", "PasswordHash", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("Users", "PhoneNumber", "NVARCHAR(50) NULL"),
                new SchemaUpdate("Users", "IsPhoneNumberConfirmed", "BIT NULL"),
                new SchemaUpdate("Users", "IsEmailConfirmed", "BIT NULL"),
                new SchemaUpdate("Users", "SecurityStamp", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("Users", "DateOfBirth", "DATETIME2 NULL"),

                // Token table updates
                new SchemaUpdate("Tokens", "AccessToken", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("Tokens", "RefreshToken", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("Tokens", "UserId", "INT NOT NULL DEFAULT 0"),
                new SchemaUpdate("Tokens", "IsImpersonate", "BIT NOT NULL DEFAULT 0"),
                new SchemaUpdate("Tokens", "OriginalUserId", "INT NULL"),
                new SchemaUpdate("Tokens", "ExpiresAt", "DATETIME2 NULL"),
                new SchemaUpdate("Tokens", "RefreshTokenExpiresAt", "DATETIME2 NULL"),
                new SchemaUpdate("Tokens", "RememberMe", "BIT NOT NULL DEFAULT 0"),
                new SchemaUpdate("Tokens", "IsRevoked", "BIT NOT NULL DEFAULT 0"),
                new SchemaUpdate("Tokens", "DeviceInfo", "NVARCHAR(500) NULL"),

                // Role table updates
                new SchemaUpdate("Roles", "Name", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Roles", "Description", "NVARCHAR(MAX) NULL"),

                // Claims table updates
                new SchemaUpdate("Claims", "Type", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Claims", "Value", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("Claims", "Issuer", "NVARCHAR(255) NULL"),
                new SchemaUpdate("Claims", "OriginalIssuer", "NVARCHAR(255) NULL"),

                // DatabaseVersions table
                new SchemaUpdate("DatabaseVersions", "Version", "NVARCHAR(50) NOT NULL DEFAULT '1.0.0'"),
                new SchemaUpdate("DatabaseVersions", "MigrationName", "NVARCHAR(255) NOT NULL DEFAULT 'Unknown'"),
                new SchemaUpdate("DatabaseVersions", "Description", "NVARCHAR(MAX) NULL"),
                new SchemaUpdate("DatabaseVersions", "AppliedOn", "DATETIME2 NOT NULL DEFAULT GETUTCDATE()"),
                new SchemaUpdate("DatabaseVersions", "AppliedBy", "NVARCHAR(255) NULL"),
                new SchemaUpdate("DatabaseVersions", "IsSuccessful", "BIT NOT NULL DEFAULT 1"),
            };

            var appliedUpdates = new List<string>();

            foreach (var update in schemaUpdates)
            {
                try
                {
                    var tableExists = await TableExistsAsync(context, update.TableName);
                    if (!tableExists) continue;

                    var columnExists = await ColumnExistsAsync(context, update.TableName, update.ColumnName);
                    if (columnExists) continue;

                    // Add the missing column
                    var sql = $"ALTER TABLE [{update.TableName}] ADD [{update.ColumnName}] {update.ColumnDefinition}";
                    await context.Database.ExecuteSqlRawAsync(sql);

                    appliedUpdates.Add($"{update.TableName}.{update.ColumnName}");
                    logger?.LogInformation("Added column: {Table}.{Column}", update.TableName, update.ColumnName);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Could not add column {Table}.{Column}", update.TableName, update.ColumnName);
                }
            }

            if (appliedUpdates.Any())
            {
                await TryRecordVersionAsync(context, CurrentSchemaVersion, "SchemaUpdate",
                    $"Added columns: {string.Join(", ", appliedUpdates)}", logger);
            }
        }

        /// <summary>
        /// Creates missing tables without affecting existing ones
        /// </summary>
        private static async Task CreateMissingTablesAsync(AppDbContext context, ILogger logger)
        {
            var tablesToCreate = new Dictionary<string, string>
            {
                ["Claims"] = @"
                    CREATE TABLE [Claims] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Type] NVARCHAR(MAX) NULL,
                        [Value] NVARCHAR(MAX) NULL,
                        [Issuer] NVARCHAR(MAX) NULL,
                        [OriginalIssuer] NVARCHAR(MAX) NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL
                    )",
                ["Roles"] = @"
                    CREATE TABLE [Roles] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Name] NVARCHAR(255) NULL,
                        [Description] NVARCHAR(MAX) NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL
                    )",
                ["Users"] = @"
                    CREATE TABLE [Users] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [FirstName] NVARCHAR(255) NULL,
                        [LastName] NVARCHAR(255) NULL,
                        [Email] NVARCHAR(255) NULL,
                        [UserName] NVARCHAR(255) NULL,
                        [Password] NVARCHAR(MAX) NULL,
                        [PasswordHash] NVARCHAR(MAX) NULL,
                        [PhoneNumber] NVARCHAR(50) NULL,
                        [IsPhoneNumberConfirmed] BIT NULL,
                        [IsEmailConfirmed] BIT NULL,
                        [SecurityStamp] NVARCHAR(MAX) NULL,
                        [DateOfBirth] DATETIME2 NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL
                    )",
                ["Tokens"] = @"
                    CREATE TABLE [Tokens] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [AccessToken] NVARCHAR(MAX) NULL,
                        [RefreshToken] NVARCHAR(MAX) NULL,
                        [UserId] INT NOT NULL,
                        [IsImpersonate] BIT NOT NULL DEFAULT 0,
                        [OriginalUserId] INT NULL,
                        [ExpiresAt] DATETIME2 NULL,
                        [RefreshTokenExpiresAt] DATETIME2 NULL,
                        [RememberMe] BIT NOT NULL DEFAULT 0,
                        [IsRevoked] BIT NOT NULL DEFAULT 0,
                        [DeviceInfo] NVARCHAR(500) NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL
                    )",
                ["UserRoles"] = @"
                    CREATE TABLE [UserRoles] (
                        [UserId] INT NOT NULL,
                        [RoleId] INT NOT NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL,
                        PRIMARY KEY ([UserId], [RoleId])
                    )",
                ["RoleClaims"] = @"
                    CREATE TABLE [RoleClaims] (
                        [RoleId] INT NOT NULL,
                        [ClaimId] INT NOT NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL,
                        PRIMARY KEY ([RoleId], [ClaimId])
                    )",
                ["VerificationCodes"] = @"
                    CREATE TABLE [VerificationCodes] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [UserId] INT NOT NULL,
                        [Code] NVARCHAR(10) NOT NULL,
                        [Type] NVARCHAR(50) NOT NULL,
                        [ExpiresAt] DATETIME2 NOT NULL,
                        [IsUsed] BIT NOT NULL DEFAULT 0,
                        [Attempts] INT NOT NULL DEFAULT 0,
                        [MaxAttempts] INT NOT NULL DEFAULT 5,
                        [SentTo] NVARCHAR(255) NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL
                    )",
                ["DatabaseVersions"] = @"
                    CREATE TABLE [DatabaseVersions] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Version] NVARCHAR(50) NOT NULL,
                        [MigrationName] NVARCHAR(255) NOT NULL,
                        [Description] NVARCHAR(MAX) NULL,
                        [AppliedOn] DATETIME2 NOT NULL,
                        [AppliedBy] NVARCHAR(255) NULL,
                        [IsSuccessful] BIT NOT NULL,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NULL
                    )"
            };

            var createdTables = new List<string>();

            foreach (var (tableName, createSql) in tablesToCreate)
            {
                try
                {
                    var exists = await TableExistsAsync(context, tableName);
                    if (exists) continue;

                    await context.Database.ExecuteSqlRawAsync(createSql);
                    createdTables.Add(tableName);
                    logger?.LogInformation("Created table: {TableName}", tableName);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Could not create table {TableName}", tableName);
                }
            }

            if (createdTables.Any())
            {
                await TryRecordVersionAsync(context, CurrentSchemaVersion, "TablesCreated",
                    $"Created tables: {string.Join(", ", createdTables)}", logger);
            }
        }

        /// <summary>
        /// Checks if a column exists in a table
        /// </summary>
        private static async Task<bool> ColumnExistsAsync(AppDbContext context, string tableName, string columnName)
        {
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName
                    ) THEN 1 ELSE 0 END";

                var tableParam = command.CreateParameter();
                tableParam.ParameterName = "@TableName";
                tableParam.Value = tableName;
                command.Parameters.Add(tableParam);

                var columnParam = command.CreateParameter();
                columnParam.ParameterName = "@ColumnName";
                columnParam.Value = columnName;
                command.Parameters.Add(columnParam);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ensures the DatabaseVersions table exists
        /// </summary>
        private static async Task EnsureVersionTableExistsAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                var versionTableExists = await TableExistsAsync(context, "DatabaseVersions");

                if (!versionTableExists)
                {
                    logger?.LogInformation("Creating DatabaseVersions table...");

                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE DatabaseVersions (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Version NVARCHAR(50) NOT NULL,
                            MigrationName NVARCHAR(255) NOT NULL,
                            Description NVARCHAR(MAX) NULL,
                            AppliedOn DATETIME2 NOT NULL,
                            AppliedBy NVARCHAR(255) NULL,
                            IsSuccessful BIT NOT NULL,
                            CreatedDate DATETIME2 NOT NULL,
                            UpdatedDate DATETIME2 NULL
                        )");

                    await TryRecordVersionAsync(context, CurrentSchemaVersion, "VersionTableCreated",
                        "DatabaseVersions table created", logger);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not ensure DatabaseVersions table exists.");
            }
        }

        /// <summary>
        /// Checks if a table exists in the database
        /// </summary>
        private static async Task<bool> TableExistsAsync(AppDbContext context, string tableName)
        {
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = @TableName
                    ) THEN 1 ELSE 0 END";

                var param = command.CreateParameter();
                param.ParameterName = "@TableName";
                param.Value = tableName;
                command.Parameters.Add(param);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to record a version entry in the database (won't throw if it fails)
        /// </summary>
        private static async Task TryRecordVersionAsync(AppDbContext context, string version,
            string migrationName, string description, ILogger logger)
        {
            try
            {
                var versionTableExists = await TableExistsAsync(context, "DatabaseVersions");
                if (!versionTableExists)
                {
                    logger?.LogDebug("DatabaseVersions table doesn't exist yet, skipping version record.");
                    return;
                }

                // Check if all required columns exist
                var hasVersionColumn = await ColumnExistsAsync(context, "DatabaseVersions", "Version");
                if (!hasVersionColumn)
                {
                    logger?.LogDebug("DatabaseVersions table is incomplete, skipping version record.");
                    return;
                }

                var versionEntry = new DatabaseVersion
                {
                    Version = version,
                    MigrationName = migrationName,
                    Description = description,
                    AppliedOn = DateTime.UtcNow,
                    AppliedBy = Environment.MachineName,
                    IsSuccessful = true,
                    CreatedDate = DateTime.UtcNow
                };

                context.DatabaseVersions.Add(versionEntry);
                await context.SaveChangesAsync();

                logger?.LogInformation("Recorded version: {Version} - {Migration}", version, migrationName);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Could not record version history (this is okay on first run).");
            }
        }

        /// <summary>
        /// Gets the current database version
        /// </summary>
        public static async Task<string> GetCurrentVersionAsync(AppDbContext context)
        {
            try
            {
                var versionTableExists = await TableExistsAsync(context, "DatabaseVersions");
                if (!versionTableExists)
                {
                    return CurrentSchemaVersion;
                }

                var latestVersion = await context.DatabaseVersions
                    .Where(v => v.IsSuccessful)
                    .OrderByDescending(v => v.AppliedOn)
                    .FirstOrDefaultAsync();

                return latestVersion?.Version ?? CurrentSchemaVersion;
            }
            catch
            {
                return CurrentSchemaVersion;
            }
        }

        /// <summary>
        /// Extracts version from migration name
        /// </summary>
        private static string GetVersionFromMigration(string migrationName)
        {
            var parts = migrationName.Split('_');
            if (parts.Length >= 1 && parts[0].Length >= 8)
            {
                var timestamp = parts[0];
                var month = timestamp.Substring(4, 2);
                var day = timestamp.Substring(6, 2);
                return $"1.{int.Parse(month)}.{int.Parse(day)}";
            }

            return CurrentSchemaVersion;
        }

        /// <summary>
        /// Represents a schema update (new column to add)
        /// </summary>
        private class SchemaUpdate
        {
            public string TableName { get; }
            public string ColumnName { get; }
            public string ColumnDefinition { get; }

            public SchemaUpdate(string tableName, string columnName, string columnDefinition)
            {
                TableName = tableName;
                ColumnName = columnName;
                ColumnDefinition = columnDefinition;
            }
        }
    }
}
