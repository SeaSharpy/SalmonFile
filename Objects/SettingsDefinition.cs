namespace Salmon.Levels.Objects;

/// <summary>Stores editor, environment, gameplay, and preview settings embedded in a level.</summary>
public sealed partial class SettingsDefinition
{
    [InspectorField("Title", Order = 0, OldOrder = -2, ID = 0, NoSave = true, MaxLength = 255)]
    internal string Title = "Untitled";

    /// <summary>Whether the level editor saves automatically.</summary>
    [InspectorField("Auto Save", Order = 1, OldOrder = -1, ID = 1)]
    public bool AutoSave = true;

    /// <summary>The translation grid interval.</summary>
    [InspectorField("Grid Snap", Order = 2, OldOrder = 1, ID = 2, Min = 0.02f, Max = 10f)]
    public float GridSize = 1f;

    /// <summary>The rotation snap interval in degrees.</summary>
    [InspectorField("Rotation Snap", Order = 3, OldOrder = 2, ID = 3, Min = 1f, Max = 90f)]
    public float RotationSnapDegrees = 15f;

    /// <summary>The saved editor camera position.</summary>
    [InspectorField(Order = 4, OldOrder = 3, ID = 4, NoInspect = true)]
    public Vector3 CameraPosition = Vector3.zero;

    /// <summary>The saved editor camera pitch in degrees.</summary>
    [InspectorField(Order = 5, OldOrder = 4, ID = 5, NoInspect = true)]
    public float CameraPitch = 0f;
    /// <summary>The saved editor camera yaw in degrees.</summary>
    [InspectorField(Order = 6, OldOrder = 4.1f, ID = 6, NoInspect = true)]
    public float CameraYaw = 0f;


    /// <summary>Whether newly created objects are placed at the local origin.</summary>
    [InspectorField("Objects Spawn at 0,0,0", Order = 7, OldOrder = 4.3f, ID = 7)]
    public bool ObjectsSpawnAtOrigin = true;

    /// <summary>The background material key.</summary>
    [InspectorField("Background Material", Order = 8, OldOrder = 5, ID = 8, Options = "Materials")]
    public string Material = "Scraper";

    /// <summary>The background type key.</summary>
    [InspectorField("Background Type", Order = 9, OldOrder = 6, ID = 9, Options = "BackgroundMeshes")]
    public string BackgroundType = "Boxy";

    /// <summary>The sky red channel in the range 0 to 1.</summary>
    [InspectorField("R", Order = 10, OldOrder = 7.1f, ID = 10, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 0.8f;

    /// <summary>The sky green channel in the range 0 to 1.</summary>
    [InspectorField("G", Order = 11, OldOrder = 7.2f, ID = 11, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 0.85f;

    /// <summary>The sky blue channel in the range 0 to 1.</summary>
    [InspectorField("B", Order = 12, OldOrder = 7.3f, ID = 12, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    /// <summary>The current numeric fog intensity.</summary>
    [InspectorField("Fog Intensity", Order = 13, OldOrder = 8.5f, ID = 13, Min = 0, Max = 20, Slider = true)]
    public int FogIntensity = 3;

    /// <summary>Whether or not to apply the above colours and fog in the editor.</summary>
    [InspectorField("Preview Mode", Order = 14, OldOrder = 8.6f, ID = 14)]
    public bool PreviewMode = true;

    /// <summary>The world-space height below which the player dies.</summary>
    [InspectorField("Death Y", Order = 15, OldOrder = 9, ID = 15, Min = -1000f, Max = 1000f, Slider = true)]
    public float DeathY = 0f;

    /// <summary>Whether style points are displayed during gameplay.</summary>
    [InspectorField("Display Style", Order = 16, OldOrder = 9.1f, ID = 16)]
    public bool DisplayStyle = true;

    /// <summary>Whether the player can jump from wall contacts.</summary>
    [InspectorField("Allow Walls", Order = 17, OldOrder = 9.2f, ID = 17)]
    public bool AllowWalls = true;

    /// <summary>Whether the player can jump during the coyote-time grace period after leaving a surface.</summary>
    [InspectorField("Allow Coyote", Order = 18, OldOrder = 9.3f, ID = 18)]
    public bool AllowCoyote = true;

    /// <summary>Whether the editor displays the preview camera guide.</summary>
    [InspectorField("Preview Guide", Order = 19, OldOrder = 10, ID = 19)]
    public bool PreviewGuide = false;

    /// <summary>Whether the game camera starts from the preview camera pose.</summary>
    [InspectorField("Game Camera Starts at Preview", Order = 20, OldOrder = 11, ID = 20)]
    public bool PreviewStart = false;
    /// <summary>The preview camera position.</summary>
    [InspectorField("Preview Position", Order = 21, OldOrder = 12, ID = 21)]
    public Vector3 PreviewPosition = Vector3.zero;

    /// <summary>The preview camera pitch in degrees.</summary>
    [InspectorField("Preview Pitch", Order = 22, OldOrder = 13, ID = 22)]
    public float PreviewPitch = 0f;

    /// <summary>The preview camera yaw in degrees.</summary>
    [InspectorField("Preview Yaw", Order = 23, OldOrder = 14, ID = 23)]
    public float PreviewYaw = 0f;

}
