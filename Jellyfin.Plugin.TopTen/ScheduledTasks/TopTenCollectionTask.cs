using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TopTen.Configuration;
using Jellyfin.Plugin.TopTen.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TopTen.ScheduledTasks
{
    /// <summary>
    /// Task that updates the Top Ten collection.
    /// </summary>
    public class TopTenCollectionTask : IScheduledTask
    {
        private readonly ILogger<TopTenCollectionTask> _logger;
        private readonly IUserManager _userManager;
        private readonly IPlaybackRankingService _rankingService;
        private readonly ICollectionSyncService _collectionSyncService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TopTenCollectionTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TopTenCollectionTask}"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="rankingService">Instance of the <see cref="IPlaybackRankingService"/> interface.</param>
        /// <param name="collectionSyncService">Instance of the <see cref="ICollectionSyncService"/> interface.</param>
        public TopTenCollectionTask(
            ILogger<TopTenCollectionTask> logger,
            IUserManager userManager,
            IPlaybackRankingService rankingService,
            ICollectionSyncService collectionSyncService)
        {
            _logger = logger;
            _userManager = userManager;
            _rankingService = rankingService;
            _collectionSyncService = collectionSyncService;
        }

        /// <inheritdoc />
        public string Name => "Update Top Ten Collection";

        /// <inheritdoc />
        public string Key => "UpdateTopTenCollection";

        /// <inheritdoc />
        public string Description => "Creates or updates a collection containing the top 10 most watched movies and TV shows from the last 30 days.";

        /// <inheritdoc />
        public string Category => "Library";

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var config = Plugin.Instance?.Configuration;
            int hours = config?.RefreshIntervalHours ?? 24;

            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.IntervalTrigger,
                    IntervalTicks = TimeSpan.FromHours(hours).Ticks,
                },
            };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);

            _logger.LogInformation("Starting Top Ten Collection update task");
            progress.Report(0);

            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-config.DaysToConsider);
            var users = _userManager.GetUsers().ToList();

            await _collectionSyncService.CleanupRenamedCollectionAsync(config, cancellationToken).ConfigureAwait(false);

            var topMovies = _rankingService.GetTopMovies(config.TopItemCount, users, cutoffDate);
            progress.Report(33);

            var topSeries = _rankingService.GetTopSeries(config.TopItemCount, users, cutoffDate);
            progress.Report(66);

            var allItems = topMovies.Cast<BaseItem>().Concat(topSeries.Cast<BaseItem>()).ToList();
            var overview = config.CollectionOverview ?? string.Empty;

            await _collectionSyncService.UpdateCollectionAsync(
                config.CollectionName, overview, allItems, cancellationToken).ConfigureAwait(false);

            if (config.PreviousCollectionName != config.CollectionName)
            {
                config.PreviousCollectionName = config.CollectionName;
                Plugin.Instance!.SaveConfiguration();
            }

            progress.Report(100);
            _logger.LogInformation("Top Ten Collection update task completed successfully");
        }
    }
}
