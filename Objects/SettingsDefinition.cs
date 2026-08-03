namespace Salmon.Levels.Objects;

/// <summary>Stores editor, environment, and preview settings embedded in a level.</summary>
public sealed partial class SettingsDefinition
{
    [InspectorField("Title", Order = -2, NoSave = true)]
    internal string Title = "Untitled";

    /// <summary>Whether the level editor saves automatically.</summary>
    [InspectorField("Auto Save", Order = -1)]
    public bool AutoSave = true;

    /// <summary>The translation grid interval.</summary>
    [InspectorField("Grid Snap", Order = 1, Min = 0.02f, Max = 10f)]
    public float GridSize = 1f;

    /// <summary>The rotation snap interval in degrees.</summary>
    [InspectorField("Rotation Snap", Order = 2, Min = 1f, Max = 90f)]
    public float RotationSnapDegrees = 15f;

    /// <summary>The saved editor camera position.</summary>
    [InspectorField("Camera Position", Order = 3, NoInspect = true)]
    public Vector3 CameraPosition = Vector3.zero;

    /// <summary>The saved editor camera pitch in degrees.</summary>
    [InspectorField("Camera Pitch", Order = 4, NoInspect = true)]
    public float CameraPitch = 0f;
    /// <summary>The saved editor camera yaw in degrees.</summary>
    [InspectorField("Camera Yaw", Order = 4.1f, NoInspect = true)]
    public float CameraYaw = 0f;


    /// <summary>Whether newly created objects are placed at the local origin.</summary>
    [InspectorField("Objects Spawn at 0,0,0", Order = 4.3f)]
    public bool ObjectsSpawnAtOrigin = true;

    /// <summary>The background material key.</summary>
    [InspectorField("Background Material", Order = 5, Options = "Materials")]
    public string Material = "Scraper";

    /// <summary>The background type key.</summary>
    [InspectorField("Background Type", Order = 6, Options = "BackgroundMeshes")]
    public string BackgroundType = "Boxy";

    /// <summary>The sky red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 7.1f, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 0.8f;

    /// <summary>The sky green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 7.2f, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 0.85f;

    /// <summary>The sky blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 7.3f, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The current numeric fog intensity.</summary>
    [InspectorField("Fog Intensity", Order = 8.5f, Min = 0, Max = 20, Slider = true)]
    public int FogIntensity = 3;

    /// <summary>The world-space height below which the player dies.</summary>
    [InspectorField("Death Y", Order = 9, Min = -1000f, Max = 1000f, Slider = true)]
    public float DeathY = 0f;

    /// <summary>Whether the editor displays the preview camera guide.</summary>
    [InspectorField("Preview Guide", Order = 10)]
    public bool PreviewGuide = false;

    /// <summary>Whether the game camera starts from the preview camera pose.</summary>
    [InspectorField("Game Camera Starts at Preview", Order = 11)]
    public bool PreviewStart = false;
    /// <summary>The preview camera position.</summary>
    [InspectorField("Preview Position", Order = 12)]
    public Vector3 PreviewPosition = Vector3.zero;

    /// <summary>The preview camera pitch in degrees.</summary>
    [InspectorField("Preview Pitch", Order = 13)]
    public float PreviewPitch = 0f;

    /// <summary>The preview camera yaw in degrees.</summary>
    [InspectorField("Preview Yaw", Order = 14)]
    public float PreviewYaw = 0f;

}
