namespace Salmon.Levels;


public sealed partial class CustomMaterialDefinition
{
    public readonly byte[] Texture = new byte[MaterialsSection.CustomMaterialByteCount];

    [InspectorField("Name", Order = 0, Old = true)]
    public string Name = "";

    [InspectorField("Blend Amount", Order = 0.5f, Old = true)]
    public byte _BlendAmount = 2;
    
    [InspectorField("Blend Amount", Order = 0.7f, Min = 0.1f, Max = 1f, Slider = true)]
    public float BlendAmount = 1f;

    [InspectorField("Tiling Size", Order = 2, Min = 0f, Max = 10f, Slider = true)]
    public float TilingSize = 1f;

    [InspectorField("Unlit", Order = 3)]
    public bool Unlit = false;

    [InspectorField("Wavy", Order = 4)]
    public bool Wavy = false;

    [InspectorField("Transparent", Order = 5)]
    public bool Transparent = false;

}

public sealed partial class MaterialsSection : LevelSection
{
    public const int CustomMaterialSize = 128;
    public const int CustomMaterialByteCount = CustomMaterialSize * CustomMaterialSize * 4;
    public CustomMaterialDefinition[] CustomMaterials = new CustomMaterialDefinition[16];

    public MaterialsSection()
    {
        for (var i = 0; i < CustomMaterials.Length; i++)
            CustomMaterials[i] = new();
    }

    public override void Read(BinaryReader reader)
    {
        foreach (var customMaterial in CustomMaterials)
        {
            if (Version < 21)
                for (var i = 0; i < CustomMaterialSize * CustomMaterialSize; i++)
                {
                    customMaterial.Texture[i * 4 + 0] = reader.ReadByte();
                    customMaterial.Texture[i * 4 + 1] = reader.ReadByte();
                    customMaterial.Texture[i * 4 + 2] = reader.ReadByte();
                    customMaterial.Texture[i * 4 + 3] = byte.MaxValue;
                }
            else
                Buffer.BlockCopy(
                    reader.ReadBytes(CustomMaterialByteCount),
                    0,
                    customMaterial.Texture,
                    0,
                    CustomMaterialByteCount
                );
            DynamicSerializer.Deserialize(typeof(CustomMaterialDefinition), customMaterial, reader);
            if (Version < 21)
                customMaterial.BlendAmount = customMaterial._BlendAmount switch
                {
                    0 => 0.25f,
                    1 => 0.5f,
                    _ => 1f,
                };
        }
    }

    public override void Write(BinaryWriter writer)
    {
        foreach (var customMaterial in CustomMaterials)
        {
            writer.Write(customMaterial.Texture);
            DynamicSerializer.Serialize(customMaterial, writer);
        }
    }

    public Dictionary<string, string> GetCustomMaterialNameMap()
    {
        var materialMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < CustomMaterials.Length; i++)
            materialMap[CustomMaterials[i].Name] = Salmon.CustomMaterials.GetKey(i);
        return materialMap;
    }
}
