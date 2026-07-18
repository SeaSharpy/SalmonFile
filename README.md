# SalmonFile

The `.salmon` file format library for SALMON levels. It does not cover the game's settings file or skin file.

[Documentation](https://seasharpy.dev/salmon/docs)

## Storage locations

SalmonFile's `StorageLocations.LevelPath` and `StorageLocations.LeaderboardPath` are relative paths. That means they resolve from the calling process's current working directory (CWD), not automatically from SALMON's Unity data directory.
For example, `Level.GetLevels(...)` reads `Levels/<mode>` beneath the CWD, and `Level.Write()` writes there. Running a tool from a repository or terminal directory can therefore create or inspect a different `Levels` tree.

Navigate to the game's data directory before running a SalmonFile-based tool when you want to work with the installed game's files.

On Windows, the default Unity data directory is:

```text
%USERPROFILE%\AppData\LocalLow\SeaSharpy\SALMON
```

For Steam Proton on Linux, the equivalent directory is inside the game's prefix. The standard Steam location for the main game (app ID `4659460`) is:

```text
~/.local/share/Steam/steamapps/compatdata/4659460/pfx/drive_c/users/steamuser/AppData/LocalLow/SeaSharpy/SALMON
```

The playtest uses app ID `4662470`. On some Linux installations the Steam root is `~/.steam/steam`, and a Steam library on another drive keeps `compatdata` under that library's `steamapps` directory.
