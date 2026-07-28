namespace Salmon.Levels;

/// <summary>Stores searchable level metadata loaded before the full level payload.</summary>
public sealed class MetadataSection : LevelSection, IDisposable
{
    /// <summary>The level title.</summary>
    [InspectorField("Title", Order = 0)]
    public string Title = "";
    /// <summary>The author name.</summary>
    [InspectorField("Author", Order = 1)]
    public string Author = "";

    /// <inheritdoc/>
    public override void Read(BinaryReader reader)
    {
        DynamicSerializer.Deserialize(typeof(MetadataSection), this, reader);
        Normalize();
    }

    /// <inheritdoc/>
    public override void Write(BinaryWriter writer)
    {
        Normalize();
        DynamicSerializer.Serialize(this, writer);
    }

    /// <inheritdoc/>
    public override void Normalize()
    {
        Title = string.IsNullOrWhiteSpace(Title) ? "Untitled Level" : Title.Trim();
        Author = string.IsNullOrWhiteSpace(Author) ? "Unknown" : Author.Trim();
    }
}
