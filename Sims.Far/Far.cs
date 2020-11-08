using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sims.Far
{
    public class Far
    {
        private readonly string pathToFar;
        public string Signature { get; set; }
        public int Version { get; set; }
        public int ManifestOffset { get; set; }
        public List<byte[]> Files { get; set; }
        public Manifest Manifest { get; set; }

        public Far(string pathToFar)
        {
            this.pathToFar = pathToFar;
            ParseFar();
        }

        private void ParseFar()
        {
            byte[] bytes = File.ReadAllBytes(this.pathToFar);
            this.Signature = Encoding.UTF8.GetString(bytes, 0, 8);
            this.Version = BitConverter.ToInt32(bytes, 8);
            this.ManifestOffset = BitConverter.ToInt32(bytes, 12);
            this.Files = new List<byte[]>();
            this.Manifest = new Manifest
            {
                NumberOfFiles = BitConverter.ToInt32(bytes, this.ManifestOffset),
                ManifestEntries = new List<ManifestEntry>()
            };

            int curOffset = this.ManifestOffset;
            for (int i = 0; i < this.Manifest.NumberOfFiles; i++)
            {
                var manifestEntry = ParseManifestEntry(bytes, curOffset + 4);
                this.Manifest.ManifestEntries.Add(manifestEntry);
                curOffset += 16 + manifestEntry.FilenameLength;

                var curFile = new byte[manifestEntry.FileLength1];
                Array.Copy(bytes, manifestEntry.FileOffset, curFile, 0, manifestEntry.FileLength1);
                this.Files.Add(curFile);
            }
        }

        private static ManifestEntry ParseManifestEntry(byte[] bytes, int offset)
        {
            var manifestEntry = new ManifestEntry
            {
                FileLength1 = BitConverter.ToInt32(bytes, offset),
                FileLength2 = BitConverter.ToInt32(bytes, offset += 4),
                FileOffset = BitConverter.ToInt32(bytes, offset += 4),
                FilenameLength = BitConverter.ToInt32(bytes, offset += 4),
            };
            manifestEntry.Filename = Encoding.UTF8.GetString(bytes, offset += 4, manifestEntry.FilenameLength);
            return manifestEntry;
        }

        /// <summary>
        /// Extract all files from the far file.
        /// </summary>
        public void Extract()
        {
            for (int i = 0; i < this.Manifest.NumberOfFiles; i++)
            {
                string dir = Path.GetDirectoryName(this.Manifest.ManifestEntries[i].Filename);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(this.Manifest.ManifestEntries[i].Filename, this.Files[i]);
            }
        }

        /// <summary>
        /// Extract files from the far file with an inclusive filter. Only files in this enumerable will be extracted.
        /// </summary>
        /// <param name="filter">Inclusive filter.</param>
        public void Extract(IEnumerable<string> filter)
        {
            for (int i = 0; i < this.Manifest.NumberOfFiles; i++)
            {
                if (!filter.Contains(this.Manifest.ManifestEntries[i].Filename))
                    continue;

                string dir = Path.GetDirectoryName(this.Manifest.ManifestEntries[i].Filename);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(this.Manifest.ManifestEntries[i].Filename, this.Files[i]);
            }
        }

        /// <summary>
        /// Extract all files from the far file to a specified directory.
        /// </summary>
        /// <param name="outputDirectory">The directory to extract files to.</param>
        public void Extract(string outputDirectory)
        {
            if (!outputDirectory.EndsWith(@"\"))
                outputDirectory += @"\";

            for (int i = 0; i < this.Manifest.NumberOfFiles; i++)
            {
                string dir = Path.GetDirectoryName(this.Manifest.ManifestEntries[i].Filename);
                if (!string.IsNullOrWhiteSpace(outputDirectory + dir) && !Directory.Exists(outputDirectory + dir))
                    Directory.CreateDirectory(outputDirectory + dir);
                File.WriteAllBytes(outputDirectory + this.Manifest.ManifestEntries[i].Filename, this.Files[i]);
            }
        }

        /// <summary>
        /// Extract files from the far file with an inclusive filter to the specified directory. 
        /// </summary>
        /// <param name="outputDirectory">The directory to extract files to.</param>
        /// <param name="filter">Inclusive filter.</param>
        public void Extract(string outputDirectory, IEnumerable<string> filter)
        {
            if (!outputDirectory.EndsWith(@"\"))
                outputDirectory += @"\";

            for (int i = 0; i < this.Manifest.NumberOfFiles; i++)
            {
                if (!filter.Contains(this.Manifest.ManifestEntries[i].Filename))
                    continue;

                string dir = Path.GetDirectoryName(this.Manifest.ManifestEntries[i].Filename);
                if (!string.IsNullOrWhiteSpace(outputDirectory + dir) && !Directory.Exists(outputDirectory + dir))
                    Directory.CreateDirectory(outputDirectory + dir);
                File.WriteAllBytes(outputDirectory + this.Manifest.ManifestEntries[i].Filename, this.Files[i]);
            }
        }
    }

    public class Manifest
    {
        public int NumberOfFiles { get; set; }
        public List<ManifestEntry> ManifestEntries { get; set; }
    }

    public class ManifestEntry
    {
        public int FileLength1 { get; set; }
        public int FileLength2 { get; set; }
        public int FileOffset { get; set; }
        public int FilenameLength { get; set; }
        public string Filename { get; set; }
    }
}
