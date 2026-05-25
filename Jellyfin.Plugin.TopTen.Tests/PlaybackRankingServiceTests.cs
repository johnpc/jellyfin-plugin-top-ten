using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.TopTen.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TopTen.Tests;

public class PlaybackRankingServiceTests
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<PlaybackRankingService> _logger;
    private readonly PlaybackRankingService _service;

    public PlaybackRankingServiceTests()
    {
        _libraryManager = Substitute.For<ILibraryManager>();
        _userDataManager = Substitute.For<IUserDataManager>();
        _logger = Substitute.For<ILogger<PlaybackRankingService>>();
        _service = new PlaybackRankingService(_libraryManager, _userDataManager, _logger);
    }

    [Fact]
    public void GetTopMovies_ReturnsEmpty_WhenNoMoviesExist()
    {
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem>());

        var result = _service.GetTopMovies(10, new List<User>(), DateTime.UtcNow.AddDays(-30));

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTopMovies_ReturnsEmpty_WhenNoRecentPlays()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Test" };
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { movie });

        var user = new User("Alice", "default", "default");
        var userData = TestHelpers.CreateUserItemData(null, 0);
        _userDataManager.GetUserData(user, movie).Returns(userData);

        var result = _service.GetTopMovies(10, new List<User> { user }, DateTime.UtcNow.AddDays(-30));

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTopMovies_RanksBy_UniqueUserCount()
    {
        var movieA = new Movie { Id = Guid.NewGuid(), Name = "Popular" };
        var movieB = new Movie { Id = Guid.NewGuid(), Name = "Less Popular" };
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { movieA, movieB });

        var user1 = new User("User1", "default", "default");
        var user2 = new User("User2", "default", "default");
        var users = new List<User> { user1, user2 };

        var recentData = TestHelpers.CreateUserItemData(DateTime.UtcNow.AddDays(-5), 1);
        var noData = TestHelpers.CreateUserItemData(null, 0);

        _userDataManager.GetUserData(user1, movieA).Returns(recentData);
        _userDataManager.GetUserData(user2, movieA).Returns(recentData);
        _userDataManager.GetUserData(user1, movieB).Returns(recentData);
        _userDataManager.GetUserData(user2, movieB).Returns(noData);

        var result = _service.GetTopMovies(10, users, DateTime.UtcNow.AddDays(-30));

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Popular");
        result[1].Name.Should().Be("Less Popular");
    }

    [Fact]
    public void GetTopMovies_ExcludesPlays_BeforeCutoff()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Old Movie" };
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { movie });

        var user = new User("User1", "default", "default");
        var oldData = TestHelpers.CreateUserItemData(DateTime.UtcNow.AddDays(-60), 1);
        _userDataManager.GetUserData(user, movie).Returns(oldData);

        var result = _service.GetTopMovies(10, new List<User> { user }, DateTime.UtcNow.AddDays(-30));

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTopMovies_RespectsCount()
    {
        var movies = Enumerable.Range(0, 20).Select(i =>
        {
            var m = new Movie { Id = Guid.NewGuid(), Name = $"Movie {i}" };
            return m;
        }).ToList();

        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(movies.Cast<BaseItem>().ToList());

        var user = new User("User1", "default", "default");
        var recentData = TestHelpers.CreateUserItemData(DateTime.UtcNow.AddDays(-1), 1);

        _userDataManager.GetUserData(Arg.Any<User>(), Arg.Any<BaseItem>()).Returns(recentData);

        var result = _service.GetTopMovies(5, new List<User> { user }, DateTime.UtcNow.AddDays(-30));

        result.Should().HaveCount(5);
    }

    [Fact]
    public void GetTopMovies_Deduplicates_ByTmdbId()
    {
        var movie1 = new Movie { Id = Guid.NewGuid(), Name = "Movie HD" };
        movie1.SetProviderId(MetadataProvider.Tmdb, "12345");
        var movie2 = new Movie { Id = Guid.NewGuid(), Name = "Movie 4K" };
        movie2.SetProviderId(MetadataProvider.Tmdb, "12345");

        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem> { movie1, movie2 });

        var user = new User("User1", "default", "default");
        var recentData = TestHelpers.CreateUserItemData(DateTime.UtcNow.AddDays(-1), 1);
        _userDataManager.GetUserData(Arg.Any<User>(), Arg.Any<BaseItem>()).Returns(recentData);

        var result = _service.GetTopMovies(10, new List<User> { user }, DateTime.UtcNow.AddDays(-30));

        result.Should().HaveCount(1);
    }

    [Fact]
    public void GetTopSeries_ReturnsEmpty_WhenNoSeriesExist()
    {
        _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(new List<BaseItem>());

        var result = _service.GetTopSeries(10, new List<User>(), DateTime.UtcNow.AddDays(-30));

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTopSeries_RanksByPlayCount_AcrossEpisodes()
    {
        var series1 = new Series { Id = Guid.NewGuid(), Name = "Binged Show" };
        var series2 = new Series { Id = Guid.NewGuid(), Name = "Casual Show" };

        var ep1 = new Episode { Id = Guid.NewGuid(), Name = "S1E1" };
        var ep2 = new Episode { Id = Guid.NewGuid(), Name = "S1E2" };
        var ep3 = new Episode { Id = Guid.NewGuid(), Name = "S1E1" };

        _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q =>
            q.IncludeItemTypes != null && q.IncludeItemTypes.Contains(Jellyfin.Data.Enums.BaseItemKind.Series)))
            .Returns(new List<BaseItem> { series1, series2 });

        _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q =>
            q.IncludeItemTypes != null && q.IncludeItemTypes.Contains(Jellyfin.Data.Enums.BaseItemKind.Episode) && q.AncestorIds != null && q.AncestorIds.Contains(series1.Id)))
            .Returns(new List<BaseItem> { ep1, ep2 });

        _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q =>
            q.IncludeItemTypes != null && q.IncludeItemTypes.Contains(Jellyfin.Data.Enums.BaseItemKind.Episode) && q.AncestorIds != null && q.AncestorIds.Contains(series2.Id)))
            .Returns(new List<BaseItem> { ep3 });

        var user = new User("User1", "default", "default");
        var recentData = TestHelpers.CreateUserItemData(DateTime.UtcNow.AddDays(-1), 1);
        _userDataManager.GetUserData(Arg.Any<User>(), Arg.Any<BaseItem>()).Returns(recentData);

        var result = _service.GetTopSeries(10, new List<User> { user }, DateTime.UtcNow.AddDays(-30));

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Binged Show");
    }
}
