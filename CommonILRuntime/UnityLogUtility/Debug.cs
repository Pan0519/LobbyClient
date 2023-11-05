
namespace UnityLogUtility
{
    public class Debug
    {
        public static void Log(object msg)
        {
            Util.Log(msg.ToString());
        }

        public static void LogWarning(object msg)
        {
            Util.LogWarning(msg.ToString());
        }

        public static void LogError(object msg)
        {
            Util.LogError(msg.ToString());
        }
    }
}
