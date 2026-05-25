Feature: Top Ten Collection

  The plugin creates a collection of the most-watched movies and series
  based on playback data within a configurable time window.

  Scenario: Movies ranked by unique user plays
    Given a library with the following movies:
      | Title          |
      | Movie A        |
      | Movie B        |
      | Movie C        |
    And the following users exist:
      | Username |
      | Alice    |
      | Bob      |
      | Charlie  |
    And the following recent playback data:
      | User    | Title   |
      | Alice   | Movie A |
      | Bob     | Movie A |
      | Charlie | Movie A |
      | Alice   | Movie B |
      | Bob     | Movie B |
      | Alice   | Movie C |
    When the Top Ten task executes
    Then the collection should contain movies in this order:
      | Title   |
      | Movie A |
      | Movie B |
      | Movie C |

  Scenario: Old playback data is excluded
    Given a library with the following movies:
      | Title          |
      | Recent Movie   |
      | Old Movie      |
    And the following users exist:
      | Username |
      | Alice    |
    And "Recent Movie" was played by "Alice" 5 days ago
    And "Old Movie" was played by "Alice" 60 days ago
    And the days to consider is 30
    When the Top Ten task executes
    Then the collection should contain "Recent Movie"
    And the collection should not contain "Old Movie"

  Scenario: Collection respects configured item count
    Given a library with 20 movies all played recently
    And the top item count is set to 5
    When the Top Ten task executes
    Then the collection should contain exactly 5 items

  Scenario: Duplicate movies across libraries are deduplicated
    Given a library with movies sharing the same TMDB ID:
      | Title         | Library | TMDB ID |
      | Movie HD      | 1080p   | 12345   |
      | Movie 4K      | 4K      | 12345   |
    And both copies have been played recently
    When the Top Ten task executes
    Then the collection should contain only one copy of the movie

  Scenario: Old collection is cleaned up when renamed
    Given an existing collection named "Old Top Ten"
    And the collection name is configured as "New Top Ten"
    And the previous collection name was "Old Top Ten"
    When the Top Ten task executes
    Then the "Old Top Ten" collection should be deleted
    And the "New Top Ten" collection should exist
