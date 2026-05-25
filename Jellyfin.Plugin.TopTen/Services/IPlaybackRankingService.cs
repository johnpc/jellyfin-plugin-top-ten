using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.TopTen.Services
{
    /// <summary>
    /// Provides methods for ranking media items by playback statistics.
    /// </summary>
    public interface IPlaybackRankingService
    {
        /// <summary>
        /// Gets the top movies ranked by playback count within the specified time period.
        /// </summary>
        /// <param name="count">The number of top movies to return.</param>
        /// <param name="users">The list of users to consider for playback data.</param>
        /// <param name="cutoffDate">The earliest date to consider for playback statistics.</param>
        /// <returns>A list of top movies ordered by playback ranking.</returns>
        List<Movie> GetTopMovies(int count, IList<User> users, DateTime cutoffDate);

        /// <summary>
        /// Gets the top series ranked by playback count within the specified time period.
        /// </summary>
        /// <param name="count">The number of top series to return.</param>
        /// <param name="users">The list of users to consider for playback data.</param>
        /// <param name="cutoffDate">The earliest date to consider for playback statistics.</param>
        /// <returns>A list of top series ordered by playback ranking.</returns>
        List<Series> GetTopSeries(int count, IList<User> users, DateTime cutoffDate);
    }
}
