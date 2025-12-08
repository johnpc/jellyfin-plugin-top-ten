# Jellyfin Top Ten Plugin

Hey Amazon Q, I'm trying to build this Jellyfin plugin that shows the top 10 most watched content, but I'm pretty new to this whole C# thing and Jellyfin plugin development. Here's what I'm thinking about so far:

## What I want to build

I want to make a plugin for Jellyfin (media server software) that automatically creates a collection of the most popular content on my server. Basically, I want to see what's being watched the most so I can recommend stuff to friends or know what's trending on my server.

## Requirements

- Need to track what movies and TV series are being watched
- For movies: count how many different users watched each movie
- For TV series: count total number of plays across all episodes
- Only count stuff watched in the last 30 days (but make this configurable)
- Create a collection in Jellyfin with the top 10 items from each movies and series (also configurable number)
- Run a scheduled task that regenerates this collection automatically every day (but make this configurable too)
