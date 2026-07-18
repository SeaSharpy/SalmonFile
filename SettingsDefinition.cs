namespace Salmon.Levels;

public sealed partial class SettingsDefinition
{
    [InspectorField("Title", Order = -2, NoSave = true)]
    public string Title = "Untitled";

    [InspectorField("Auto Save", Order = -1)]
    public bool AutoSave = true;

    [InspectorField("Grid Snap", Order = 1, Min = 0.01f, Max = 10f)]
    public float GridSize = 1f;

    [InspectorField("Rotation Snap", Order = 2, Min = 1f, Max = 90f)]
    public float RotationSnapDegrees = 15f;

    [InspectorField("Camera Position", Order = 3, NoInspect = true)]
    public Vector3 CameraPosition = Vector3.zero;

    [InspectorField("Camera Pitch", Order = 4, NoInspect = true)]
    public float CameraPitch = 0f;
    [InspectorField("Camera Yaw", Order = 4.1f, NoInspect = true)]
    public float CameraYaw = 0f;


    [InspectorField("Objects Spawn at 0,0,0", Order = 4.3f)]
    public bool ObjectsSpawnAtOrigin = true;

    [InspectorField("Background Material", Order = 5, Options = "Materials")]
    public string Material = "Scraper";

    [InspectorField("Background Type", Order = 6, Options = "BackgroundMeshes")]
    public string BackgroundType = "Boxy";

    [InspectorField("R", Order = 7.1f, Min = 0f, Max = 1f, Slider = true)]
    public float Red = 0.8f;

    [InspectorField("G", Order = 7.2f, Min = 0f, Max = 1f, Slider = true)]
    public float Green = 0.85f;

    [InspectorField("B", Order = 7.3f, Min = 0f, Max = 1f, Slider = true)]
    public float Blue = 1f;

    [InspectorField("Fog Intensity", Order = 8, Old = true)]
    public string _FogIntensity = "Default";

    [InspectorField("Fog Intensity", Order = 8.5f, Min = 0, Max = 20, Slider = true)]
    public int FogIntensity = 3;

    [InspectorField("Death Y", Order = 9, Min = -1000f, Max = 1000f, Slider = true)]
    public float DeathY = 0f;

    [InspectorField("Preview Guide", Order = 10)]
    public bool PreviewGuide = false;

    [InspectorField("Game Camera Starts at Preview", Order = 11)]
    public bool PreviewStart = false;
    [InspectorField("Preview Position", Order = 12)]
    public Vector3 PreviewPosition = Vector3.zero;

    [InspectorField("Preview Pitch", Order = 13)]
    public float PreviewPitch = 0f;

    [InspectorField("Preview Yaw", Order = 14)]
    public float PreviewYaw = 0f;

    [InspectorButton("Set Preview")]
    public void Preview()
    {
        PreviewPosition = CameraPosition;
        PreviewPitch = CameraPitch;
        PreviewYaw = CameraYaw;
    }

}
