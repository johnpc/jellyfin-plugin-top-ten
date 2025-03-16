# Jellyfin TopTen Plugin

This plugin creates and maintains a collection of the top 10 most watched movies and TV shows on your Jellyfin server within a configurable time period (default: last 30 days).

## Features

- Creates a scheduled task that runs every 24 hours (configurable)
- Identifies the top 10 movies and series watched on the Jellyfin server within the last 30 days (configurable)
- Creates a collection named "Jellyfin Top Ten" (configurable)
- For shows, uses the number of total plays for the series within the time period
- For movies, uses the number of unique users who have played the movie within the time period
- Keeps the collection up to date by adding new popular content and removing items that have fallen out of the top ten

## Install Process

1. In Jellyfin, go to `Dashboard -> Plugins -> Catalog -> Gear Icon (upper left)` add and a repository.
1. Set the Repository name to @johnpc (Top Ten)
1. Set the Repository URL to https://raw.githubusercontent.com/johnpc/jellyfin-plugin-top-ten/refs/heads/main/manifest.json
1. Click "Save"
1. Go to Catalog and search for Top Ten
1. Click on it and install
1. Restart Jellyfin

## User Guide

1. To set it up, visit `Dashboard -> Plugins -> My Plugins -> Top Ten -> Settings`
1. Configure your preferences
1. Choose "Save"
1. In Scheduled Tasks, execute "Update Top Ten Collection"
1. Viola! Your Top Ten Collection now exists!

## Installation

1. Download the latest release from the Jellyfin plugin catalog or build from source
2. Install the plugin in Jellyfin
3. Configure the plugin settings if needed
4. The scheduled task will run automatically according to the configured schedule

## Configuration

- **Collection Name**: The name of the collection to create (default: "Jellyfin Top Ten")
- **Number of Items**: How many items to include in the collection (default: 10)
- **Refresh Interval**: How often to update the collection in hours (default: 24)
- **Days to Consider**: Only count plays within this many days (default: 30)

## Building from Source

1. Clone this repository
2. Ensure you have .NET 8.0 SDK installed
3. Build the solution:
   ```
   dotnet build Jellyfin.Plugin.TopTen/Jellyfin.Plugin.TopTen.csproj -p:TreatWarningsAsErrors=false
   ```
4. The plugin DLL will be in the `Jellyfin.Plugin.TopTen/bin/Debug/net8.0` directory
