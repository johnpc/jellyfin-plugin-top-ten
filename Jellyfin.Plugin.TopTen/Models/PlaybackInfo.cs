using System;

namespace Jellyfin.Plugin.TopTen.Models
{
    /// <summary>
    /// Class to store playback information for media items.
    /// </summary>
    public class PlaybackInfo
    {
        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the item name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the play count.
        /// </summary>
        public int PlayCount { get; set; }

        /// <summary>
        /// Gets or sets the number of unique users who played this item.
        /// </summary>
        public int UniqueUserCount { get; set; }
    }
}
