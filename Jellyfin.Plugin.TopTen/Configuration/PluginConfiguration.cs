using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TopTen.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            CollectionName = "Jellyfin Top Ten";
            TopItemCount = 10;
            RefreshIntervalHours = 24;
            DaysToConsider = 30;
        }

        /// <summary>
        /// Gets or sets the name of the collection to create.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the number of top items to include.
        /// </summary>
        public int TopItemCount { get; set; }

        /// <summary>
        /// Gets or sets the refresh interval in hours.
        /// </summary>
        public int RefreshIntervalHours { get; set; }

        /// <summary>
        /// Gets or sets the number of days to consider for playback statistics.
        /// </summary>
        public int DaysToConsider { get; set; }
    }
}
