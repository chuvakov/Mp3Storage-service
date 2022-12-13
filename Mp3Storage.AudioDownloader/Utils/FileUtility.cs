using System.Reflection;

namespace Mp3Storage.AudioDownloader.Utils
{
    public static class FileUtility
    {
        public static string GetPathTo(string fileName)
        {
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(appDir, fileName);
        }

        public static void CreateFileIfNotExist(string path, string text = "")
        {
            if (!File.Exists(path))
            {
                using var file = File.CreateText(path);
                file.WriteLine(text);
            }
        }
    }
}
