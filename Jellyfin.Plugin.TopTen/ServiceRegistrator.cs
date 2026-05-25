using Jellyfin.Plugin.TopTen.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TopTen
{
    /// <summary>
    /// Registers plugin services with the DI container.
    /// </summary>
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IPlaybackRankingService, PlaybackRankingService>();
            serviceCollection.AddSingleton<ICollectionSyncService, CollectionSyncService>();
        }
    }
}
