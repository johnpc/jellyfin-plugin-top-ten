using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.TopTen.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TopTen.Services
{
    /// <summary>
    /// Service that synchronizes collections with ranked media items.
    /// </summary>
    public class CollectionSyncService : ICollectionSyncService
    {
        private static readonly BaseItemKind[] BoxSetKinds = new[] { BaseItemKind.BoxSet };

        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly ILogger<CollectionSyncService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSyncService"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="collectionManager">Instance of the <see cref="ICollectionManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{CollectionSyncService}"/> interface.</param>
        public CollectionSyncService(
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            ILogger<CollectionSyncService> logger)
        {
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task UpdateCollectionAsync(string collectionName, string overview, List<BaseItem> items, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating collection: {CollectionName} with {Count} items", collectionName, items.Count);

            var results = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = BoxSetKinds,
                Name = collectionName,
            });
            var collection = results.Count > 0 ? results[0] as BoxSet : null;

            if (collection == null)
            {
                _logger.LogInformation("Creating new collection: {CollectionName}", collectionName);
                collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = collectionName,
                    ItemIdList = new List<string>(),
                    IsLocked = true,
                }).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(overview) && collection.Overview != overview)
            {
                collection.Overview = overview;
                await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            }

            var currentItems = collection.GetLinkedChildren();
            var currentItemIds = currentItems.Select(i => i.Id).ToList();

            var itemsToAdd = items.Where(i => !currentItemIds.Contains(i.Id)).ToList();
            var itemsToRemove = currentItems.Where(i => !items.Any(newItem => newItem.Id == i.Id)).ToList();

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

        /// <inheritdoc />
        public Task CleanupRenamedCollectionAsync(PluginConfiguration config, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(config.PreviousCollectionName) || config.PreviousCollectionName == config.CollectionName)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("Collection renamed from '{OldName}' to '{NewName}', cleaning up old collection", config.PreviousCollectionName, config.CollectionName);

            var oldResults = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = BoxSetKinds,
                Name = config.PreviousCollectionName,
            });
            var oldCollection = oldResults.Count > 0 ? oldResults[0] as BoxSet : null;

            if (oldCollection != null)
            {
                _logger.LogInformation("Deleting old collection: {Name}", config.PreviousCollectionName);
                _libraryManager.DeleteItem(oldCollection, new DeleteOptions { DeleteFileLocation = true });
            }

            return Task.CompletedTask;
        }
    }
}
