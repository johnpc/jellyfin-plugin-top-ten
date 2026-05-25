using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.TopTen.Configuration;
using Jellyfin.Plugin.TopTen.Services;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TopTen.Tests;

public class CollectionSyncServiceTests
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<CollectionSyncService> _logger;
    private readonly CollectionSyncService _service;

    public CollectionSyncServiceTests()
    {
        _libraryManager = Substitute.For<ILibraryManager>();
        _collectionManager = Substitute.For<ICollectionManager>();
        _logger = Substitute.For<ILogger<CollectionSyncService>>();
        _service = new CollectionSyncService(_libraryManager, _collectionManager, _logger);
    }

    [Fact]
    public async Task CleanupRenamedCollectionAsync_DoesNothing_WhenPreviousNameIsEmpty()
    {
        var config = new PluginConfiguration { PreviousCollectionName = "", CollectionName = "Top Ten" };

        await _service.CleanupRenamedCollectionAsync(config, CancellationToken.None);

        _libraryManager.DidNotReceive().GetItemList(Arg.Any<InternalItemsQuery>());
    }

    [Fact]
    public async Task CleanupRenamedCollectionAsync_DoesNothing_WhenNamesMatch()
    {
        var config = new PluginConfiguration { PreviousCollectionName = "Top Ten", CollectionName = "Top Ten" };

        await _service.CleanupRenamedCollectionAsync(config, CancellationToken.None);

        _libraryManager.DidNotReceive().GetItemList(Arg.Any<InternalItemsQuery>());
    }

    [Fact]
    public async Task CleanupRenamedCollectionAsync_DeletesOldCollection_WhenRenamed()
    {
        var config = new PluginConfiguration { PreviousCollectionName = "Old Name", CollectionName = "New Name" };
        var oldCollection = new BoxSet { Id = Guid.NewGuid(), Name = "Old Name" };

        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { oldCollection });

        await _service.CleanupRenamedCollectionAsync(config, CancellationToken.None);

        _libraryManager.Received(1).DeleteItem(oldCollection, Arg.Any<DeleteOptions>());
    }

    [Fact]
    public async Task CleanupRenamedCollectionAsync_DoesNotDelete_WhenCollectionNotFound()
    {
        var config = new PluginConfiguration { PreviousCollectionName = "Gone", CollectionName = "New" };

        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem>());

        await _service.CleanupRenamedCollectionAsync(config, CancellationToken.None);

        _libraryManager.DidNotReceive().DeleteItem(Arg.Any<BaseItem>(), Arg.Any<DeleteOptions>());
    }

    [Fact]
    public async Task UpdateCollectionAsync_CreatesNewCollection_WhenNoneExists()
    {
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem>());

        var newCollection = new BoxSet { Id = Guid.NewGuid(), Name = "Top Ten" };
        _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
            .Returns(newCollection);

        await _service.UpdateCollectionAsync("Top Ten", "", new List<BaseItem>(), CancellationToken.None);

        await _collectionManager.Received(1).CreateCollectionAsync(Arg.Is<CollectionCreationOptions>(o => o.Name == "Top Ten"));
    }
}
