using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TopTen.Configuration;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.TopTen.Services
{
    /// <summary>
    /// Provides methods for synchronizing collections with ranked media items.
    /// </summary>
    public interface ICollectionSyncService
    {
        /// <summary>
        /// Updates or creates a collection with the specified items.
        /// </summary>
        /// <param name="collectionName">The name of the collection to update or create.</param>
        /// <param name="overview">The overview text for the collection.</param>
        /// <param name="items">The items to include in the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateCollectionAsync(string collectionName, string overview, List<BaseItem> items, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a previously named collection when the collection name has been changed.
        /// </summary>
        /// <param name="config">The plugin configuration containing old and new collection names.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CleanupRenamedCollectionAsync(PluginConfiguration config, CancellationToken cancellationToken);
    }
}
