using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.TopTen.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TopTen.Services
{
    /// <summary>
    /// Service that ranks media items based on playback statistics.
    /// </summary>
    public class PlaybackRankingService : IPlaybackRankingService
    {
        private static readonly BaseItemKind[] MovieKinds = new[] { BaseItemKind.Movie };
        private static readonly BaseItemKind[] SeriesKinds = new[] { BaseItemKind.Series };
        private static readonly BaseItemKind[] EpisodeKinds = new[] { BaseItemKind.Episode };

        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger<PlaybackRankingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackRankingService"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{PlaybackRankingService}"/> interface.</param>
        public PlaybackRankingService(
            ILibraryManager libraryManager,
            IUserDataManager userDataManager,
            ILogger<PlaybackRankingService> logger)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public List<Movie> GetTopMovies(int count, IList<User> users, DateTime cutoffDate)
        {
            _logger.LogInformation("Finding top {Count} movies since {CutoffDate}", count, cutoffDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = MovieKinds,
                Recursive = true,
            })
            .Cast<Movie>()
            .ToList();

            var moviePlaybackInfo = new Dictionary<Guid, PlaybackInfo>();

            foreach (var movie in movies)
            {
                var playbackInfo = new PlaybackInfo
                {
                    Id = movie.Id,
                    Name = movie.Name,
                    PlayCount = 0,
                    UniqueUserCount = 0,
                };

                foreach (var user in users)
                {
                    var userData = _userDataManager.GetUserData(user, movie);

                    if (userData != null && userData.LastPlayedDate.HasValue && userData.LastPlayedDate.Value >= cutoffDate)
                    {
                        playbackInfo.UniqueUserCount++;
                        playbackInfo.PlayCount++;
                    }
                }

                moviePlaybackInfo[movie.Id] = playbackInfo;
            }

            return movies
                .Where(m => moviePlaybackInfo.ContainsKey(m.Id) && moviePlaybackInfo[m.Id].UniqueUserCount > 0)
                .OrderByDescending(m => moviePlaybackInfo[m.Id].UniqueUserCount)
                .ThenByDescending(m => moviePlaybackInfo[m.Id].PlayCount)
                .GroupBy(m => m.GetProviderId(MetadataProvider.Tmdb) ?? m.GetProviderId(MetadataProvider.Imdb) ?? m.Id.ToString())
                .Select(g => g.First())
                .Take(count)
                .ToList();
        }

        /// <inheritdoc />
        public List<Series> GetTopSeries(int count, IList<User> users, DateTime cutoffDate)
        {
            _logger.LogInformation("Finding top {Count} series since {CutoffDate}", count, cutoffDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

            var series = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = SeriesKinds,
                Recursive = true,
            })
            .Cast<Series>()
            .ToList();

            var seriesPlaybackInfo = new Dictionary<Guid, PlaybackInfo>();

            foreach (var s in series)
            {
                var playbackInfo = new PlaybackInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    PlayCount = 0,
                    UniqueUserCount = 0,
                };

                var episodes = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = EpisodeKinds,
                    Recursive = true,
                    AncestorIds = new[] { s.Id },
                })
                .Cast<Episode>();

                var uniqueUsers = new HashSet<Guid>();

                foreach (var episode in episodes)
                {
                    foreach (var user in users)
                    {
                        var userData = _userDataManager.GetUserData(user, episode);

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

            return series
                .Where(s => seriesPlaybackInfo.ContainsKey(s.Id) && seriesPlaybackInfo[s.Id].PlayCount > 0)
                .OrderByDescending(s => seriesPlaybackInfo[s.Id].PlayCount)
                .ThenByDescending(s => seriesPlaybackInfo[s.Id].UniqueUserCount)
                .GroupBy(s => s.GetProviderId(MetadataProvider.Tmdb) ?? s.GetProviderId(MetadataProvider.Tvdb) ?? s.Id.ToString())
                .Select(g => g.First())
                .Take(count)
                .ToList();
        }
    }
}
