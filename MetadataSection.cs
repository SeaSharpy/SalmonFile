namespace Salmon.Levels;

public sealed class MetadataSection : LevelSection, IDisposable
{
    [InspectorField("Title", Order = 0)]
    public string Title = "";
    [InspectorField("Author", Order = 1)]
    public string Author = "";

    public override void Read(BinaryReader reader)
    {
        if (Version < 20)
        {
            reader.ReadString();
            Title = reader.ReadString();
            Author = reader.ReadString();
        }
        else DynamicSerializer.Deserialize(typeof(MetadataSection), this, reader);
        Normalize();
    }

    public override void Write(BinaryWriter writer)
    {
        Normalize();
        DynamicSerializer.Serialize(this, writer);
    }

    public override void Normalize()
    {
        Title = string.IsNullOrWhiteSpace(Title) ? "Untitled Level" : Title.Trim();
        Author = string.IsNullOrWhiteSpace(Author) ? "Unknown" : Author.Trim();
    }
}
