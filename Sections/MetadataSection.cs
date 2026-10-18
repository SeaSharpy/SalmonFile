namespace Salmon.Levels.Sections;

/// <summary>Stores searchable level metadata loaded before the full level payload.</summary>
public sealed class MetadataSection : LevelSection, IDisposable
{
    /// <summary>The level title.</summary>
    [InspectorField(Order = 0, OldOrder = 0, ID = 0)]
    public string Title = "";
    /// <summary>The author name.</summary>
    [InspectorField(Order = 1, OldOrder = 1, ID = 1)]
    public string Author = "";

    internal override void Read(BinaryReader reader)
    {
        DynamicSerializer.Deserialize(typeof(MetadataSection), this, reader);
        Normalize();
    }

    internal override void Write(BinaryWriter writer)
    {
        Normalize();
        DynamicSerializer.Serialize(this, writer);
    }

    /// <inheritdoc/>
    public override void Normalize()
    {
        Title = string.IsNullOrWhiteSpace(Title) ? "Untitled Level" : Title.Trim();
        if (Title.Length > 255)
            Title = Title[..255];
        Author = string.IsNullOrWhiteSpace(Author) ? "Unknown" : Author.Trim();
    }
}
