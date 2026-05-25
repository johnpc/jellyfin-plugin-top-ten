using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.TopTen.ScheduledTasks;
using Jellyfin.Plugin.TopTen.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.TopTen.Tests;

public class TopTenCollectionTaskTests
{
    private readonly ILogger<TopTenCollectionTask> _logger;
    private readonly IUserManager _userManager;
    private readonly IPlaybackRankingService _rankingService;
    private readonly ICollectionSyncService _collectionSyncService;
    private readonly TopTenCollectionTask _task;

    public TopTenCollectionTaskTests()
    {
        _logger = Substitute.For<ILogger<TopTenCollectionTask>>();
        _userManager = Substitute.For<IUserManager>();
        _rankingService = Substitute.For<IPlaybackRankingService>();
        _collectionSyncService = Substitute.For<ICollectionSyncService>();

        _task = new TopTenCollectionTask(
            _logger,
            _userManager,
            _rankingService,
            _collectionSyncService);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        _task.Name.Should().Be("Update Top Ten Collection");
    }

    [Fact]
    public void Key_ReturnsExpectedValue()
    {
        _task.Key.Should().Be("UpdateTopTenCollection");
    }

    [Fact]
    public void Category_ReturnsLibrary()
    {
        _task.Category.Should().Be("Library");
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        _task.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetDefaultTriggers_ReturnsIntervalTrigger()
    {
        var triggers = _task.GetDefaultTriggers().ToList();

        triggers.Should().HaveCount(1);
        triggers[0].Type.Should().Be(TaskTriggerInfoType.IntervalTrigger);
        triggers[0].IntervalTicks.Should().Be(TimeSpan.FromHours(24).Ticks);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenProgressIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _task.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEarly_WhenConfigIsNull()
    {
        var progress = Substitute.For<IProgress<double>>();

        await _task.ExecuteAsync(progress, CancellationToken.None);

        await _collectionSyncService.DidNotReceive()
            .UpdateCollectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<List<BaseItem>>(), Arg.Any<CancellationToken>());
    }
}
