using System.IO;
using System.IO.Compression;

namespace v2rayN.Helpers.RandomFileGeneratorContainer
{
    public static class RandomFileGenerator
    {

        private const int BufferSize = 1024 * 1024;
        private const int FileSize = 10 * BufferSize;

        public static string GenerateRandomFile()
        {
            var buffer = new byte[BufferSize];
            var random = new Random();

            var fileName = Utils.GetPath("output.zip");
            using var fileStream = File.Create(fileName);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
            var entry = archive.CreateEntry(Utils.GetPath("random.bin"));

            using var entryStream = entry.Open();
            for (var i = 0; i < FileSize / BufferSize; i++)
            {
                random.NextBytes(buffer);
                entryStream.Write(buffer, 0, buffer.Length);
            }
            return fileName;
        }
    }
}