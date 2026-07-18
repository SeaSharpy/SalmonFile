# SalmonFile

The `.salmon` file format library for SALMON levels. It does not cover the game's settings file or skin file.

[Documentation](https://seasharpy.dev/salmon/docs)

## Creating and saving a level

Create a `Level`, generate its numeric ID with `Snowflake.Create()`, then add
objects to its root group. Do not let a user choose the ID when the level may be
uploaded: the upload service accepts numeric IDs, and `Snowflake.Create()`
generates the expected form without user input.

```csharp
using Salmon;
using Salmon.Levels;
using System.Numerics;

using var level = new Level
{
    ID = Snowflake.Create(),
    Title = "My First Level",
    Author = "Your Name",
    Mode = StoredMode.Created,
};

level.Root.Name = "Root";
level.Root.Objects.Add(new Wall
{
    Name = "Floor",
    Position = new Vector3(0f, -0.5f, 5f),
    Scale = new Vector3(10f, 1f, 20f),
});
level.Root.Objects.Add(new CheckpointObjectDefinition
{
    Name = "Start",
    Position = new Vector3(0f, 1f, 0f),
});
level.Root.Objects.Add(new WinObjectDefinition
{
    Name = "Finish",
    Position = new Vector3(0f, 1f, 10f),
});

level.Write();
```

`Write()` normalizes the level, creates the destination directory, and writes
the file to `Levels/Created/<generated ID>.salmon` relative to the process's
current working directory. Other `StoredMode` values select the corresponding
subdirectory. The upload service overwrites `Author` with the authenticated
uploader's identity, so the local value does not control online attribution.

To write somewhere outside the standard storage tree, provide your own writer:

```csharp
using System.IO;

using var stream = File.Create(
    Path.Combine(@"C:\Levels", $"{level.ID}.salmon")
);
using var writer = new BinaryWriter(stream);
level.Write(writer);
```

## Loading and updating a level

`Level.Open(...)` initially reads only the header, metadata, and preview. Call
`Load()` before accessing objects, settings, or materials. Open levels own a
file stream, so always dispose them.

```csharp
using System;
using Salmon.Levels;

static void UpdateLevel(string id)
{
    var mode = StoredMode.Created;
    var path = System.IO.Path.Combine(
        Salmon.StorageLocations.LevelPath,
        mode.ToString(),
        $"{id}.salmon"
    );

    using var level = Level.Open(path);
    level.Mode = mode;
    level.Load();

    Console.WriteLine($"{level.Title} by {level.Author}");
    foreach (var levelObject in level.Root.Recurse())
        Console.WriteLine(levelObject.Name);

    level.Title = "My Updated Level";
    level.Write();
}
```

Set `Mode` explicitly after opening a level if it is not in `Created`, because
the parameterless `Write()` derives its destination from `Mode` and `ID`.
Pass the existing level's ID when loading; generating a new ID would address a
different file.
Use `Level.TryOpen(...)` when invalid or missing files should produce `false`
instead of throwing.

## Storage locations

SalmonFile's `StorageLocations.LevelPath` and `StorageLocations.LeaderboardPath` are relative paths. That means they resolve from the calling process's current working directory (CWD), not automatically from SALMON's Unity data directory.
For example, `Level.Write()` writes beneath `Levels/<mode>` in the CWD. Running a tool from a repository or terminal directory can therefore create or inspect a different `Levels` tree.

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
