#if !DEMO_APP
namespace Salmon.Levels.Sections;

/// <summary>Serializes level editor and environment settings.</summary>
public sealed class SettingsSection : LevelSection
{
    /// <summary>The settings stored in this section.</summary>
    public SettingsDefinition Settings = new();

    internal override void Write(BinaryWriter writer) => DynamicSerializer.Serialize(Settings, writer);

    internal override void Read(BinaryReader reader)
    {
        Settings = (SettingsDefinition)DynamicSerializer.Deserialize(typeof(SettingsDefinition), reader);
    }
}
#endif
