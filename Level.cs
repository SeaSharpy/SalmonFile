namespace Salmon.Levels;

/// <summary>Identifies the storage category containing a level.</summary>
public enum StoredMode : byte
{
    /// <summary>A level created or edited locally.</summary>
    Created,
    /// <summary>A level downloaded from the online browser.</summary>
    Downloaded,
    /// <summary>A built-in main level.</summary>
    Main,
}

/// <summary>Represents a versioned <c>.salmon</c> level and its lazily loaded sections.</summary>
public sealed partial class Level : IDisposable
{
    private const int CurrentVersion = 21;
    private const int MinVersion = 7;
    private const string Magic = "SALMONLEVEL";
    private readonly Dictionary<SectionKind, SectionHeader> Sections = [];
    /// <summary>The open stream backing this level, or <see langword="null"/> for a new level.</summary>
    public FileStream Stream;
    /// <summary>The reader associated with <see cref="Stream"/>, or <see langword="null"/> for a new level.</summary>
    public BinaryReader Reader;
    private bool Loaded;
    private int Version = CurrentVersion;
    private string IDInternal = "";

    /// <summary>The level file's last modification time in UTC.</summary>
    public DateTime LastModifiedUTC;
    /// <summary>The level's metadata section.</summary>
    public MetadataSection Metadata { get; }
    /// <summary>The level editor settings section.</summary>
    public SettingsSection SettingsSection { get; private set; }
    /// <summary>The object hierarchy section.</summary>
    public ObjectsSection ObjectsSection { get; private set; }
    /// <summary>The level preview image section.</summary>
    public PreviewSection Preview { get; }
    /// <summary>The custom materials section.</summary>
    public MaterialsSection Materials { get; private set; }
    /// <summary>The storage category used to resolve <see cref="Path"/>.</summary>
    public StoredMode Mode = StoredMode.Created;
    /// <summary>The level identifier used as the file name.</summary>
    public string ID { get => IDInternal; set => IDInternal = value ?? ""; }
    /// <summary>The normalized level title.</summary>
    public string Title { get => Metadata.Title; set => Metadata.Title = value ?? ""; }
    /// <summary>The normalized level author.</summary>
    public string Author { get => Metadata.Author; set => Metadata.Author = value ?? ""; }
    /// <summary>The root object group.</summary>
    public Group Root { get => ObjectsSection.Root; set => ObjectsSection.Root = value ?? new(); }
    /// <summary>The level editor settings.</summary>
    public SettingsDefinition Settings { get => SettingsSection.Settings; set => SettingsSection.Settings = value ?? new(); }

    public Level()
    {
        Metadata = Attach(new MetadataSection());
        SettingsSection = Attach(new SettingsSection());
        ObjectsSection = Attach(new ObjectsSection());
        Preview = Attach(new PreviewSection());
        Materials = Attach(new MaterialsSection());
    }

    /// <summary>Opens a level file and reads its header, metadata, and preview.</summary>
    /// <param name="path">The path to the <c>.salmon</c> file.</param>
    /// <returns>The partially loaded level.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidDataException">The file header or version is invalid.</exception>
    public static Level Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be empty.", "path");

        var level = new Level
        {
            ID = System.IO.Path.GetFileNameWithoutExtension(path),
            LastModifiedUTC = File.GetLastWriteTimeUtc(path),
            Stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read),
        };
        level.Reader = new BinaryReader(level.Stream);
        level.ReadInitial();
        return level;
    }

    /// <summary>Attempts to open a level without propagating file or format errors.</summary>
    /// <param name="path">The path to the <c>.salmon</c> file.</param>
    /// <param name="level">Receives the partially loaded level when successful.</param>
    /// <returns><see langword="true"/> when the level was opened; otherwise <see langword="false"/>.</returns>
    public static bool TryOpen(string path, out Level level)
    {
        level = null;
        try
        {
            level = Open(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Reads the file header and eagerly loads the metadata and preview sections.</summary>
    public void ReadInitial()
    {
        try
        {
            ReadHeader();
            LoadSection(SectionKind.Metadata, Metadata);
            LoadSection(SectionKind.Preview, Preview);
            Metadata.Normalize();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal static Level[] GetLevels(StoredMode mode, Level reusableLevel = null)
    {
        var paths = Directory.GetFiles(System.IO.Path.Combine(StorageLocations.LevelPath, mode.ToString()), $"*.salmon");
        var levels = new List<Level>(paths.Length);

        for (var i = 0; i < paths.Length; i++)
        {
            if (reusableLevel != null && reusableLevel.Mode == mode && string.Equals(System.IO.Path.GetFileNameWithoutExtension(paths[i]), reusableLevel.ID, StringComparison.OrdinalIgnoreCase))
            {
                levels.Add(reusableLevel);
                continue;
            }

            if (!TryOpen(paths[i], out var level))
                continue;

            level.Mode = mode;
            levels.Add(level);
        }

        if (mode == StoredMode.Main)
            levels.Sort(CompareLevelsByName);
        else
            levels.Sort(CompareLevelsByRecentFirst);

        return levels.ToArray();
    }

    internal static int CompareLevelsByName(Level left, Level right)
    {
        var titleComparison = string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
        if (titleComparison != 0)
            return titleComparison;

        return string.Compare(left.ID, right.ID, StringComparison.OrdinalIgnoreCase);
    }

    internal static int CompareLevelsByRecentFirst(Level left, Level right)
    {
        var modifiedComparison = right.LastModifiedUTC.CompareTo(left.LastModifiedUTC);
        if (modifiedComparison != 0)
            return modifiedComparison;

        return CompareLevelsByName(left, right);
    }
    /// <summary>The level path relative to <see cref="StorageLocations.LevelPath"/>.</summary>
    public string Path => System.IO.Path.Combine(StorageLocations.LevelPath, Mode.ToString(), $"{ID}.salmon");
    /// <summary>Writes the complete current-format level to a binary writer.</summary>
    /// <param name="outWriter">The destination writer.</param>
    public void Write(BinaryWriter outWriter)
    {
        Load();
        Version = CurrentVersion;
        Normalize();
        var payloads = new (SectionKind kind, byte[] bytes)[5]
        {
            (SectionKind.Metadata, Metadata.Write()),
            (SectionKind.Preview, Preview.Write()),
            (SectionKind.Materials, Materials.Write()),
            (SectionKind.Settings, SettingsSection.Write()),
            (SectionKind.Objects, ObjectsSection.Write()),
        };

        using var header = new MemoryStream();
        using (var writer = new BinaryWriter(header, Encoding.UTF8, true))
            WriteHeader(writer, null, payloads.Length);

        var offset = header.Length;

        var sections = payloads.Select(payload =>
        {
            var section = (payload.kind, offset, payload.bytes);
            offset += payload.bytes.Length;
            return section;
        }).ToArray();

        header.SetLength(0);
        using (var writer = new BinaryWriter(header, Encoding.UTF8, true))
            WriteHeader(writer, sections, sections.Length);

        outWriter.Write(header.GetBuffer(), 0, (int)header.Length);
        for (var i = 0; i < sections.Length; i++)
            outWriter.Write(sections[i].bytes, 0, sections[i].bytes.Length);
    }

    /// <summary>Writes the level to <see cref="Path"/>, replacing its current contents.</summary>
    public void Write()
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
            Write(writer);

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path) ?? StorageLocations.LevelPath);
        EnsureWritableStream();
        Stream.Position = 0;
        Stream.SetLength(0);
        Stream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        Stream.Flush();
        LastModifiedUTC = File.GetLastWriteTimeUtc(Path);
        Stream.Position = 0;
        Sections.Clear();
        ReadHeader();
    }
    /// <summary>Closes and deletes the level file at <see cref="Path"/>.</summary>
    public void Delete()
    {
        Dispose();
        File.Delete(Path);
    }

    /// <summary>Lazily loads materials, settings, and objects.</summary>
    /// <returns>This level.</returns>
    public Level Load()
    {
        if (Loaded)
            return this;

        LoadSection(SectionKind.Materials, Materials);
        LoadSection(SectionKind.Settings, SettingsSection);
        LoadSection(SectionKind.Objects, ObjectsSection);
        Normalize();
        Loaded = true;
        return this;
    }

    /// <summary>Releases loaded section data while keeping metadata and preview available.</summary>
    public void Unload()
    {
        SettingsSection.Dispose();
        ObjectsSection.Dispose();
        Materials.Dispose();
        SettingsSection = Attach(new SettingsSection());
        ObjectsSection = Attach(new ObjectsSection());
        Materials = Attach(new MaterialsSection());
        Loaded = false;
    }

    /// <summary>Normalizes all section data before use or serialization.</summary>
    public void Normalize()
    {
        Metadata.Normalize();
        Preview.Normalize();
        Materials.Normalize();
        ObjectsSection.Normalize();
        SettingsSection.Normalize();
    }

    /// <summary>Closes the backing file and disposes every section.</summary>
    public void Dispose()
    {
        CloseReader();
        Metadata.Dispose();
        Preview.Dispose();
        Materials.Dispose();
        ObjectsSection.Dispose();
        SettingsSection.Dispose();
    }


    private void ReadHeader()
    {
        var magic = Reader.ReadString();
        if (magic != Magic)
            throw new InvalidDataException("The level binary header is invalid.");

        Version = Reader.ReadInt32();
        if (Version < MinVersion || Version > CurrentVersion)
            throw new InvalidDataException($"The level binary header is invalid. Version {Version} is not supported.");
        var count = Reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var kind = (SectionKind)Reader.ReadByte();
            var offset = Reader.ReadInt64();
            var length = Reader.ReadInt64();
            Sections[kind] = new SectionHeader(offset, length);
        }
    }

    private void EnsureWritableStream()
    {
        if (Stream != null)
        {
            if (!Stream.CanWrite)
                throw new InvalidOperationException("The level stream is not writable.");
            if (Reader == null)
                Reader = new BinaryReader(Stream);
            return;
        }

        Stream = File.Open(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        Reader = new BinaryReader(Stream);
    }

    private void CloseReader()
    {
        Reader?.Dispose();
        Stream?.Dispose();
        Reader = null;
        Stream = null;
    }

    private static void WriteHeader(BinaryWriter writer, (SectionKind kind, long offset, byte[] bytes)[] sections, int sectionCount)
    {
        writer.Write(Magic);
        writer.Write(CurrentVersion);
        writer.Write(sectionCount);
        for (var i = 0; i < sectionCount; i++)
        {
            if (sections == null)
            {
                writer.Write((byte)0);
                writer.Write((long)0);
                writer.Write((long)0);
                continue;
            }
            writer.Write((byte)sections[i].kind);
            writer.Write(sections[i].offset);
            writer.Write((long)sections[i].bytes.Length);
        }
    }

    private void LoadSection(SectionKind kind, LevelSection section)
    {
        if (section == null || !Sections.TryGetValue(kind, out var header))
            return;
        try
        {
            section.Read(Reader, header, Version);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private T Attach<T>(T section) where T : LevelSection
    {
        section.Level = this;
        return section;
    }

    private enum SectionKind : byte
    {
        Metadata = 1,
        Settings = 2,
        Objects = 3,
        Preview = 4,
        Materials = 5,
    }
}
