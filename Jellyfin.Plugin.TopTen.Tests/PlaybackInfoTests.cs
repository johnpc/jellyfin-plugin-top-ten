using System;
using FluentAssertions;
using Jellyfin.Plugin.TopTen.Models;
using Xunit;

namespace Jellyfin.Plugin.TopTen.Tests;

public class PlaybackInfoTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var info = new PlaybackInfo();

        info.Id.Should().Be(Guid.Empty);
        info.Name.Should().BeEmpty();
        info.PlayCount.Should().Be(0);
        info.UniqueUserCount.Should().Be(0);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var info = new PlaybackInfo
        {
            Id = id,
            Name = "Test Movie",
            PlayCount = 5,
            UniqueUserCount = 3
        };

        info.Id.Should().Be(id);
        info.Name.Should().Be("Test Movie");
        info.PlayCount.Should().Be(5);
        info.UniqueUserCount.Should().Be(3);
    }
}
