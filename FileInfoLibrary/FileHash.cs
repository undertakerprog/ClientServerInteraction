using System.Security.Cryptography;

namespace FileInfoLibrary
{
    public static class FileHash
    {
        public static string ComputeFileHash(string filePath)
        {
            var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        }
    }
}
