using System;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.TopTen.Tests;

public static class TestHelpers
{
    public static UserItemData CreateUserItemData(DateTime? lastPlayedDate = null, int playCount = 0)
    {
        return new UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            LastPlayedDate = lastPlayedDate,
            PlayCount = playCount
        };
    }
}
