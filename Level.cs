namespace Salmon.Levels;

public enum StoredMode : byte
{
    Created,
    Downloaded,
    Main,
}

[Serializable]
public sealed partial class Level : IDisposable
{
    public Level()
    {
        
    }

    public const int CurrentVersion = 21;
    public const int MinVersion = 7;
    private const string Magic = "SALMONLEVEL";
    private readonly Dictionary<SectionKind, SectionHeader> Sections = [];
    public FileStream Stream;
    public BinaryReader Reader;
    private bool Loaded;
    private int Version = CurrentVersion;
    private string IDInternal = "";

    public DateTime LastModifiedUTC;
    public MetadataSection Metadata { get; } = new();
    public SettingsSection SettingsSection { get; private set; } = new();
    public ObjectsSection ObjectsSection { get; private set; } = new();
    public PreviewSection Preview { get; } = new();
    public MaterialsSection Materials { get; private set; } = new();
    public StoredMode Mode = StoredMode.Created;
    public string ID { get => IDInternal; set => IDInternal = value ?? ""; }
    public string Title { get => Metadata.Title; set => Metadata.Title = value ?? ""; }
    public string Author { get => Metadata.Author; set => Metadata.Author = value ?? ""; }
    public Group Root { get => ObjectsSection.Root; set => ObjectsSection.Root = value ?? new(); }
    public SettingsDefinition Settings { get => SettingsSection.Settings; set => SettingsSection.Settings = value ?? new(); }

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

    public static Level[] GetLevels(StoredMode mode, Level reusableLevel = null)
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

    public static int CompareLevelsByName(Level left, Level right)
    {
        var titleComparison = string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
        if (titleComparison != 0)
            return titleComparison;

        return string.Compare(left.ID, right.ID, StringComparison.OrdinalIgnoreCase);
    }

    public static int CompareLevelsByRecentFirst(Level left, Level right)
    {
        var modifiedComparison = right.LastModifiedUTC.CompareTo(left.LastModifiedUTC);
        if (modifiedComparison != 0)
            return modifiedComparison;

        return CompareLevelsByName(left, right);
    }
    public string Path => System.IO.Path.Combine(StorageLocations.LevelPath, Mode.ToString(), $"{ID}.salmon");
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
    public void Delete()
    {
        Dispose();
        File.Delete(Path);
    }

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

    public void Unload()
    {
        SettingsSection.Dispose();
        ObjectsSection.Dispose();
        Materials.Dispose();
        SettingsSection = new();
        ObjectsSection = new();
        Materials = new();
        Loaded = false;
    }

    public void Normalize()
    {
        Metadata.Normalize();
        Preview.Normalize();
        Materials.Normalize();
        ObjectsSection.Normalize();
        SettingsSection.Normalize();
    }

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
        section.Level = this;
        try
        {
            section.Read(Reader, header, Version);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
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
