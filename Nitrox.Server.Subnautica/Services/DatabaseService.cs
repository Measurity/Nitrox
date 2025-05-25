using System;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nitrox.Server.Subnautica.Database;
using Nitrox.Server.Subnautica.Models.Configuration;
using Nitrox.Server.Subnautica.Models.Events;
using Nitrox.Server.Subnautica.Models.Events.Core;
using Nitrox.Server.Subnautica.Models.Persistence;
using NitroxModel.Helper;

namespace Nitrox.Server.Subnautica.Services;

/// <summary>
///     Initializes the database and provides access to it.
/// </summary>
internal sealed class DatabaseService(IDbContextFactory<WorldDbContext> dbContextFactory, ITrigger<ISeeDbInitialized, object> dbInitTrigger, IOptions<ServerStartOptions> startOptionsProvider, ILogger<DatabaseService> logger)
    : IHostedLifecycleService, IPersistState
{
    private readonly Lock saveLocker = new();
    private readonly IDbContextFactory<WorldDbContext> dbContextFactory = dbContextFactory;
    private readonly TaskCompletionSource dbInit = new();
    private readonly ILogger<DatabaseService> logger = logger;
    private readonly IOptions<ServerStartOptions> startOptionsProvider = startOptionsProvider;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await InitDatabase(cancellationToken);
            }
            catch (Exception ex)
            {
                dbInit.TrySetException(ex);
                throw;
            }
            if (!dbInit.TrySetResult())
            {
                throw new Exception("Failed to init database");
            }

            // Notify other parts of code that database is ready.
            await dbInitTrigger.Trigger();
        }, cancellationToken).ContinueWithHandleError(exception => logger.ZLogCritical(exception, $"Database initialization error"));
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => InnerSave();

    public async Task<WorldDbContext> GetDbContextAsync()
    {
        await dbInit.Task; // Ensures database is initialized before other code can access it via context.
        return await dbContextFactory.CreateDbContextAsync();
    }

    public async Task<bool> Save(string fileName = "world.db", bool throttle = true)
    {
        if (!throttle)
        {
            return await InnerSave(fileName);
        }
        ThrottleHelper.Throttle(TimeSpan.FromSeconds(15), async void () =>
        {
            try
            {
                await InnerSave(fileName);
            }
            catch (Exception e)
            {
                logger.ZLogError(e, $"");
            }
        });
        return true;
    }

    private async Task<bool> InnerSave(string fileName = "world.db")
    {
        logger.ZLogInformation($"Saving database...");
        ServerStartOptions options = startOptionsProvider.Value;
        string mainSaveFilePath = Path.Combine(options.GetServerSavePath(), fileName);
        try
        {
            await using WorldDbContext db = await GetDbContextAsync();

            // Save to server save location.
            lock (saveLocker)
            {
                if (!db.SqliteSave(mainSaveFilePath))
                {
                    logger.ZLogCritical($"Failed to save database to {mainSaveFilePath.ToSensitive():@FilePath}");
                    return false;
                }

                logger.ZLogInformation($"Saved database to {mainSaveFilePath.ToSensitive():@FilePath}");

                // Create a copy into backup folder.
                // TODO: Backup config file
                // TODO: Change this to use options (e.g. MaxBackups)
                string backupFileName = DateTimeOffset.Now.ToString("O").Replace("T", " ");
                backupFileName = backupFileName.ReplaceInvalidFileNameCharacters();
                if (Path.GetExtension(backupFileName).Length > 4)
                {
                    backupFileName = backupFileName.Replace('.', '\'');
                }
                backupFileName = Path.ChangeExtension(backupFileName, ".db");
                Directory.CreateDirectory(options.GetServerSaveBackupsPath());
                File.Copy(mainSaveFilePath, Path.Combine(options.GetServerSaveBackupsPath(), backupFileName));
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.ZLogError(ex, $"Error while trying to save the database");
        }

        return false;
    }

    private async Task InitDatabase(CancellationToken cancellationToken = default)
    {
        await using WorldDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        string databaseFilePath = Path.Combine(startOptionsProvider.Value.GetServerSavePath(), "world.db");
        try
        {
            if (db.SqliteLoad(databaseFilePath))
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.ZLogError(ex, $"Something is blocking the SQLite database. Check that you do not have it open in your IDE or other database viewer.");
            throw;
        }
    }

    Task IPersistState.PersistState() => Save();
}
