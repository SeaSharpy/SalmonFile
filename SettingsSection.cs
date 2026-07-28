namespace Salmon.Levels;

/// <summary>Serializes level editor and environment settings.</summary>
public sealed class SettingsSection : LevelSection
{
    /// <summary>The settings stored in this section.</summary>
    public SettingsDefinition Settings = new();

    /// <inheritdoc/>
    public override void Write(BinaryWriter writer) => DynamicSerializer.Serialize(Settings, writer);

    /// <inheritdoc/>
    public override void Read(BinaryReader reader)
    {
        Settings = (SettingsDefinition)DynamicSerializer.Deserialize(typeof(SettingsDefinition), reader);
        var materialMap = Level.Materials.GetCustomMaterialNameMap();
        if (materialMap.TryGetValue(Settings.Material, out var materialKey))
            Settings.Material = materialKey;
    }
}
