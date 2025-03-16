using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.TopTen.Configuration;
using Jellyfin.Plugin.TopTen.Models;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;

        private static readonly BaseItemKind[] MovieKinds = new[] { BaseItemKind.Movie };
        private static readonly BaseItemKind[] SeriesKinds = new[] { BaseItemKind.Series };
        private static readonly BaseItemKind[] EpisodeKinds = new[] { BaseItemKind.Episode };
        private static readonly BaseItemKind[] BoxSetKinds = new[] { BaseItemKind.BoxSet };

        /// <summary>
        /// Initializes a new instance of the <see cref="TopTenCollectionTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public TopTenCollectionTask(
            ILogger<TopTenCollectionTask> logger,
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
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
            // Run task every 24 hours
            var config = Plugin.Instance?.Configuration;
            int hours = config?.RefreshIntervalHours ?? 24;
            
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromHours(hours).Ticks
                }
            };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            _logger.LogInformation("Starting Top Ten Collection update task");
            progress.Report(0);

            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.LogError("Plugin configuration is null");
                return;
            }

            var collectionName = config.CollectionName;
            var topCount = config.TopItemCount;
            var daysToConsider = config.DaysToConsider;
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToConsider);

            try
            {
                // Get all users
                var users = _userManager.Users.ToList();
                
                // Get top movies based on unique user plays
                var topMovies = GetTopMovies(topCount, users, cutoffDate);
                progress.Report(33);

                // Get top series based on total play count
                var topSeries = GetTopSeries(topCount, cutoffDate);
                progress.Report(66);

                // Create or update the collection
                await UpdateCollectionAsync(collectionName, topMovies.Concat<BaseItem>(topSeries).ToList(), cancellationToken).ConfigureAwait(false);
                progress.Report(100);

                _logger.LogInformation("Top Ten Collection update task completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Top Ten Collection");
                throw;
            }
        }

        private List<Movie> GetTopMovies(int count, List<User> users, DateTime cutoffDate)
        {
            _logger.LogInformation("Finding top {Count} movies since {CutoffDate}", count, cutoffDate.ToString("yyyy-MM-dd"));
            
            try
            {
                // Get all movies
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = MovieKinds,
                    Recursive = true
                })
                .Cast<Movie>()
                .ToList();

                // Create PlaybackInfo objects for each movie
                var moviePlaybackInfo = new Dictionary<Guid, PlaybackInfo>();
                
                foreach (var movie in movies)
                {
                    var playbackInfo = new PlaybackInfo
                    {
                        Id = movie.Id,
                        Name = movie.Name,
                        PlayCount = 0,
                        UniqueUserCount = 0
                    };

                    // Count unique users who have played this movie within the cutoff period
                    foreach (var user in users)
                    {
                        // Check if user has played this movie by looking at their UserData
                        var userData = _userDataManager.GetUserData(user, movie);
                        
                        // Only count plays that happened after the cutoff date
                        if (userData != null && userData.LastPlayedDate.HasValue && userData.LastPlayedDate.Value >= cutoffDate)
                        {
                            playbackInfo.UniqueUserCount++;
                            
                            // Try to estimate recent play count based on last played date
                            // This is an approximation since Jellyfin doesn't store play dates for each play
                            playbackInfo.PlayCount++;
                        }
                    }
                    
                    moviePlaybackInfo[movie.Id] = playbackInfo;
                }

                // Get top movies by unique user count
                return movies
                    .Where(m => moviePlaybackInfo.ContainsKey(m.Id) && moviePlaybackInfo[m.Id].UniqueUserCount > 0)
                    .OrderByDescending(m => moviePlaybackInfo[m.Id].UniqueUserCount)
                    .ThenByDescending(m => moviePlaybackInfo[m.Id].PlayCount)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top movies");
                return new List<Movie>();
            }
        }

        private List<Series> GetTopSeries(int count, DateTime cutoffDate)
        {
            _logger.LogInformation("Finding top {Count} series since {CutoffDate}", count, cutoffDate.ToString("yyyy-MM-dd"));
            
            try
            {
                // Get all series
                var series = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = SeriesKinds,
                    Recursive = true
                })
                .Cast<Series>()
                .ToList();

                // Create PlaybackInfo objects for each series
                var seriesPlaybackInfo = new Dictionary<Guid, PlaybackInfo>();
                
                foreach (var s in series)
                {
                    var playbackInfo = new PlaybackInfo
                    {
                        Id = s.Id,
                        Name = s.Name,
                        PlayCount = 0,
                        UniqueUserCount = 0
                    };

                    // Get all episodes for this series
                    var episodes = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        IncludeItemTypes = EpisodeKinds,
                        Recursive = true,
                        AncestorIds = new[] { s.Id }
                    })
                    .Cast<Episode>();

                    // Track unique users who watched this series within the cutoff period
                    var uniqueUsers = new HashSet<Guid>();

                    // Sum up play counts from all episodes within the cutoff period
                    foreach (var episode in episodes)
                    {
                        foreach (var user in _userManager.Users)
                        {
                            var userData = _userDataManager.GetUserData(user, episode);
                            
                            // Only count plays that happened after the cutoff date
                            if (userData != null && userData.LastPlayedDate.HasValue && userData.LastPlayedDate.Value >= cutoffDate)
                            {
                                playbackInfo.PlayCount++;
                                uniqueUsers.Add(user.Id);
                            }
                        }
                    }
                    
                    playbackInfo.UniqueUserCount = uniqueUsers.Count;
                    seriesPlaybackInfo[s.Id] = playbackInfo;
                }

                // Get top series by total play count within the period
                return series
                    .Where(s => seriesPlaybackInfo.ContainsKey(s.Id) && seriesPlaybackInfo[s.Id].PlayCount > 0)
                    .OrderByDescending(s => seriesPlaybackInfo[s.Id].PlayCount)
                    .ThenByDescending(s => seriesPlaybackInfo[s.Id].UniqueUserCount)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top series");
                return new List<Series>();
            }
        }

        private async Task UpdateCollectionAsync(string collectionName, List<BaseItem> items, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating collection: {CollectionName} with {Count} items", collectionName, items.Count);
            
            try
            {
                // Find existing collection or create a new one
                var collection = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = BoxSetKinds,
                    Name = collectionName
                })
                .FirstOrDefault() as BoxSet;

                if (collection == null)
                {
                    _logger.LogInformation("Creating new collection: {CollectionName}", collectionName);
                    collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                    {
                        Name = collectionName,
                        ItemIdList = new List<string>(),
                        IsLocked = true
                    }).ConfigureAwait(false);
                }

                // Get current items in the collection
                var currentItems = collection.GetLinkedChildren();
                var currentItemIds = currentItems.Select(i => i.Id).ToList();
                
                // Get items to add and remove
                var itemsToAdd = items.Where(i => !currentItemIds.Contains(i.Id)).ToList();
                var itemsToRemove = currentItems.Where(i => !items.Any(newItem => newItem.Id == i.Id)).ToList();

                // Update collection
                if (itemsToAdd.Count > 0)
                {
                    _logger.LogInformation("Adding {Count} items to collection", itemsToAdd.Count);
                    await _collectionManager.AddToCollectionAsync(collection.Id, itemsToAdd.Select(i => i.Id).ToArray())
                        .ConfigureAwait(false);
                }

                if (itemsToRemove.Count > 0)
                {
                    _logger.LogInformation("Removing {Count} items from collection", itemsToRemove.Count);
                    await _collectionManager.RemoveFromCollectionAsync(collection.Id, itemsToRemove.Select(i => i.Id).ToArray())
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating collection");
                throw;
            }
        }
    }
}
