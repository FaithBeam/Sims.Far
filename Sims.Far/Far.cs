using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sims.Far
{
    /// <summary>
    /// The FAR format (.far files) are used to bundle (archive) multiple files together. All numeric values in the header and manifest are stored in little-endian order(least significant byte first).
    /// </summary>
    public class Far
    {
        private readonly string _pathToFar;
        /// <summary>
        /// The signature is an eight-byte string, consisting literally of "FAR!byAZ" (without the quotes).
        /// </summary>
        public string Signature { get; set; }
        /// <summary>
        /// The version is always one.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// The manifest offset is the byte offset from the beginning of the file to the manifest.
        /// The contents of the archived files are simply concatenated together without any other structure or padding.Caveat: all of the files observed have been a multiple of four in length, so it's possible that the files may be padded to a two-byte or four-byte boundary and the case has simply never been encountered.
        /// </summary>
        public int ManifestOffset { get; set; }
        /// <summary>
        /// The manifest contains a count of the number of archived files, followed by an entry for each file. In all of the examples examined the order of the entries matches the order of the archived files, but whether this is a firm requirement or not is unknown.
        /// </summary>
        public Manifest Manifest { get; set; }
        /// <summary>
        /// A stream of the far file. The consumer is expected to dispose this.
        /// </summary>
        public Stream FarStream { get; set; }

        /// <summary>
        /// The Far constructor.
        /// </summary>
        /// <param name="pathToFar">The path to the far file.</param>
        public Far(string pathToFar)
        {
            _pathToFar = pathToFar;
        }

        /// <summary>
        /// This method parses the far file for contents.
        /// </summary>
        public void ParseFar()
        {
            FarStream = new FileStream(_pathToFar, FileMode.Open, FileAccess.Read);
            var bytes = new byte[8];
            FarStream.Read(bytes, 0, 8);
            Signature = Encoding.UTF8.GetString(bytes);

            bytes = new byte[4];
            FarStream.Read(bytes, 0, 4);
            Version = BitConverter.ToInt32(bytes, 0);

            FarStream.Read(bytes, 0, 4);
            ManifestOffset = BitConverter.ToInt32(bytes, 0);

            FarStream.Seek(ManifestOffset, SeekOrigin.Begin);
            FarStream.Read(bytes, 0, 4);
            Manifest = new Manifest
            {
                NumberOfFiles = BitConverter.ToInt32(bytes, 0),
                ManifestEntries = new List<ManifestEntry>()
            };

            for (int i = 0; i < Manifest.NumberOfFiles; i++)
            {
                var manifestEntry = ParseManifestEntry();
                Manifest.ManifestEntries.Add(manifestEntry);
            }
        }

        private ManifestEntry ParseManifestEntry()
        {
            var bytes = new byte[4];
            var manifestEntry = new ManifestEntry();

            FarStream.Read(bytes, 0, 4);
            manifestEntry.FileLength1 = BitConverter.ToInt32(bytes, 0);

            FarStream.Read(bytes, 0, 4);
            manifestEntry.FileLength2 = BitConverter.ToInt32(bytes, 0);

            FarStream.Read(bytes, 0, 4);
            manifestEntry.FileOffset = BitConverter.ToInt32(bytes, 0);

            FarStream.Read(bytes, 0, 4);
            manifestEntry.FilenameLength = BitConverter.ToInt32(bytes, 0);

            bytes = new byte[manifestEntry.FilenameLength];
            FarStream.Read(bytes, 0, manifestEntry.FilenameLength);
            manifestEntry.Filename = Encoding.UTF8.GetString(bytes);

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
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            // Create a default filter if one wasn't supplied
            filter = filter ?? new string[0];

            foreach (var entry in Manifest.ManifestEntries)
            {
                if (filter.Count() > 0 && !filter.Contains(entry.Filename, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (preserveDirectories && entry.Filename.Contains(Path.DirectorySeparatorChar))
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(outputDirectory, entry.Filename)));

                var bytes = new byte[entry.FileLength1];
                FarStream.Seek(entry.FileOffset, SeekOrigin.Begin);
                FarStream.Read(bytes, 0, entry.FileLength1);
                if (preserveDirectories)
                {
                    using (var file = new FileStream(Path.Combine(outputDirectory, entry.Filename), FileMode.Create, FileAccess.Write))
                    {
                        file.Write(bytes, 0, entry.FileLength1);
                    }
                }
                else
                {
                    using (var file = new FileStream(Path.Combine(outputDirectory, Path.GetFileName(entry.Filename)), FileMode.Create, FileAccess.Write))
                    {
                        file.Write(bytes, 0, entry.FileLength1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// The manifest contains a count of the number of archived files, followed by an entry for each file. In all of the examples examined the order of the entries matches the order of the archived files, but whether this is a firm requirement or not is unknown.
    /// </summary>
    public class Manifest
    {
        /// <summary>
        /// The number of files in the far file.
        /// </summary>
        public int NumberOfFiles { get; set; }
        /// <summary>
        /// A list of Manifest Entries in the far file.
        /// </summary>
        public List<ManifestEntry> ManifestEntries { get; set; }
    }

    /// <summary>
    /// A manifest entry containing the first file length, second file length, file offset, file name length, and file name.
    /// </summary>
    public class ManifestEntry
    {
        /// <summary>
        /// The file length is stored twice. Perhaps this is because some variant of FAR files supports compressed data and the fields would hold the compressed and uncompressed sizes, but this is pure speculation. The safest thing to do is to leave the fields identical.
        /// </summary>
        public int FileLength1 { get; set; }
        /// <summary>
        /// The file length is stored twice. Perhaps this is because some variant of FAR files supports compressed data and the fields would hold the compressed and uncompressed sizes, but this is pure speculation. The safest thing to do is to leave the fields identical.
        /// </summary>
        public int FileLength2 { get; set; }
        /// <summary>
        /// The file offset is the byte offset from the beginning of the FAR file to the archived file.
        /// </summary>
        public long FileOffset { get; set; }
        /// <summary>
        /// The filename length is the number of bytes in the filename. Filenames are stored without a terminating null. For example, the filename "foo" would have a filename length of three and the entry would be nineteen bytes long in total.
        /// </summary>
        public int FilenameLength { get; set; }
        /// <summary>
        /// The name of the file. This can include directories.
        /// </summary>
        public string Filename { get; set; }
    }
}
