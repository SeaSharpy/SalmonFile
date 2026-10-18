namespace Salmon.Levels.Sections;


/// <summary>Defines a custom RGBA material embedded in a level.</summary>
public sealed partial class CustomMaterialDefinition
{
    /// <summary>The packed 128^2 RGBA texture pixels.</summary>
    public readonly byte[] Texture = new byte[MaterialsSection.CustomMaterialByteCount];

    /// <exclude />
    [InspectorField(Order = 0, OldOrder = 0, ID = 0, Old = true)]
    public string Name = "";

    /// <exclude />
    [InspectorField(Order = 1, OldOrder = 0.5f, ID = 1, Old = true)]
    public byte _BlendAmount = 2;
    
    /// <summary>The material blend amount.</summary>
    [InspectorField("Blend Amount", Order = 2, OldOrder = 0.7f, ID = 2, Min = 0.1f, Max = 1f, Slider = true)]
    public float BlendAmount = 1f;

    /// <summary>The texture tiling size.</summary>
    [InspectorField("Tiling Size", Order = 3, OldOrder = 2, ID = 3, Min = 0f, Max = 10f, Slider = true)]
    public float TilingSize = 1f;

    /// <summary>Whether the material ignores lighting.</summary>
    [InspectorField("Unlit", Order = 4, OldOrder = 3, ID = 4)]
    public bool Unlit = false;

    /// <summary>Whether the material uses the wave effect.</summary>
    [InspectorField("Wavy", Order = 5, OldOrder = 4, ID = 5)]
    public bool Wavy = false;

    /// <summary>Whether the material uses transparency or alpha cutout.</summary>
    [InspectorField("Transparent", Order = 6, OldOrder = 5, ID = 6)]
    public bool Transparent = false;

}

/// <summary>Stores the fixed set of custom materials embedded in a level.</summary>
public sealed partial class MaterialsSection : LevelSection
{
    /// <summary>The width and height of every custom material texture.</summary>
    public const int CustomMaterialSize = 128;
    /// <summary>The byte count of a packed RGBA custom material texture.</summary>
    public const int CustomMaterialByteCount = CustomMaterialSize * CustomMaterialSize * 4;
    /// <summary>The level's sixteen custom material definitions.</summary>
    public CustomMaterialDefinition[] CustomMaterials = new CustomMaterialDefinition[16];

    /// <summary>Creates a material section with all custom material slots initialized.</summary>
    public MaterialsSection()
    {
        for (var i = 0; i < CustomMaterials.Length; i++)
            CustomMaterials[i] = new();
    }

    internal override void Read(BinaryReader reader)
    {
        foreach (var customMaterial in CustomMaterials)
        {
            Buffer.BlockCopy(
                reader.ReadBytes(CustomMaterialByteCount),
                0,
                customMaterial.Texture,
                0,
                CustomMaterialByteCount
            );
            DynamicSerializer.Deserialize(typeof(CustomMaterialDefinition), customMaterial, reader);
        }
    }

    internal override void Write(BinaryWriter writer)
    {
        foreach (var customMaterial in CustomMaterials)
        {
            writer.Write(customMaterial.Texture);
            DynamicSerializer.Serialize(customMaterial, writer);
        }
    }
}
