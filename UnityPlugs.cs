public static class Vector3Extensions
{
    extension(Vector3 v)
    {
        public float x
        {
            get => v.X;
            set => v.X = value;
        }
        public float y
        {
            get => v.Y;
            set => v.Y = value;
        }
        public float z
        {
            get => v.Z;
            set => v.Z = value;
        }
        public static Vector3 zero => Vector3.Zero;
        public static Vector3 one => Vector3.One;
    }

    extension(Quaternion q)
    {
        public float x
        {
            get => q.X;
            set => q.X = value;
        }
        public float y
        {
            get => q.Y;
            set => q.Y = value;
        }
        public float z
        {
            get => q.Z;
            set => q.Z = value;
        }
        public float w
        {
            get => q.W;
            set => q.W = value;
        }
    }
}
public static class Debug
{
    public static void Log(object message) => Console.WriteLine(message);
    public static void LogException(Exception exception) => Console.WriteLine(exception);
    public static void LogWarning(object message) => Console.WriteLine(message);
}

namespace Salmon
{
    public static class CustomMaterials
    {
        public static string GetKey(int index) => $"Custom {index + 1}";
    }
    public static class StorageLocations
    {
        public static string LevelPath => "Levels";
        public static string LeaderboardPath => "Leaderboards";
    }
}
