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
        /// Extract files from the far file.
        /// </summary>
        /// <param name="outputDirectory">The directory to extract the files to. Otherwise it will extract them at the current directory.</param>
        /// <param name="filter">An inclusive filter. Use this if you want to extract only certain files from the far. Entries must be exact.</param>
        /// <param name="preserveDirectories">Whether or not to create the directories from a filename. If a filename was Community\Bus_loadscreen_800x600.bmp, Community\ would or wouldn't be created depending on this parameter. true = create it, false = strip it.</param>
        public void Extract(string outputDirectory = "", IEnumerable<string> filter = null, bool preserveDirectories = true)
        {
            // Create an empty list if one isn't supplied by the user
            filter = filter ?? new List<string>();

            // Add directory separator if the output directory doesn't have one at the end and create it.
            if (!string.IsNullOrWhiteSpace(outputDirectory) && !outputDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())){
                outputDirectory += Path.DirectorySeparatorChar;
                Directory.CreateDirectory(outputDirectory);
            }

            // Loop through the files in the far file
            for (int i = 0; i < this.Manifest.NumberOfFiles; i++)
            {
                // Skip files that are not in the filter. An empty filter is ignored.
                if (filter.Count() > 0 && !filter.Contains(this.Manifest.ManifestEntries[i].Filename))
                    continue;

                if (preserveDirectories)
                {
                    // Get the directory to this file and create it
                    string dir = Path.GetDirectoryName(this.Manifest.ManifestEntries[i].Filename);
                    if (!string.IsNullOrWhiteSpace(outputDirectory + dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(outputDirectory + dir);

                    // Write the file
                    File.WriteAllBytes(outputDirectory + this.Manifest.ManifestEntries[i].Filename, this.Files[i]);
                }
                else {
                    // Write the file without its directory. If internally a filename was "Community\Bus_loadscreen_800x600.bmp", it would be created without the Community folder
                    File.WriteAllBytes(outputDirectory + Path.GetFileName(this.Manifest.ManifestEntries[i].Filename), this.Files[i]);
                }
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
