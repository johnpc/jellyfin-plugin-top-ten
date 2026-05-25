using FluentAssertions;
using Jellyfin.Plugin.TopTen.Configuration;
using Xunit;

namespace Jellyfin.Plugin.TopTen.Tests;

public class PluginConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var config = new PluginConfiguration();

        config.CollectionName.Should().Be("Jellyfin Top Ten");
        config.TopItemCount.Should().Be(10);
        config.RefreshIntervalHours.Should().Be(24);
        config.DaysToConsider.Should().Be(30);
        config.CollectionOverview.Should().BeEmpty();
        config.PreviousCollectionName.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var config = new PluginConfiguration
        {
            CollectionName = "My Top Movies",
            TopItemCount = 5,
            RefreshIntervalHours = 12,
            DaysToConsider = 7,
            CollectionOverview = "Best of the week",
            PreviousCollectionName = "Old Name"
        };

        config.CollectionName.Should().Be("My Top Movies");
        config.TopItemCount.Should().Be(5);
        config.RefreshIntervalHours.Should().Be(12);
        config.DaysToConsider.Should().Be(7);
        config.CollectionOverview.Should().Be("Best of the week");
        config.PreviousCollectionName.Should().Be("Old Name");
    }
}
