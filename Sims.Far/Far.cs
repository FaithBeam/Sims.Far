using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sims.Far.Exceptions;

namespace Sims.Far
{
    /// <summary>
    /// The FAR format (.far files) are used to bundle (archive) multiple files together. All numeric values in the header and manifest are stored in little-endian order(least significant byte first).
    /// </summary>
    public class Far : IFar
    {
        /// <summary>
        /// Path to the far file to work with.
        /// </summary>
        public string PathToFar { get; set; }
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
        /// The Far constructor.
        /// </summary>
        /// <param name="pathToFar">The path to the far file.</param>
        public Far(string pathToFar)
        {
            PathToFar = pathToFar;
            ParseFar();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Far()
        {
        }

        /// <summary>
        /// Parse the far file
        /// </summary>
        public void ParseFar()
        {
            using (var stream = new FileStream(PathToFar, FileMode.Open, FileAccess.Read))
            {
                var bytes = new byte[8];
                stream.Read(bytes, 0, 8);
                Signature = Encoding.UTF8.GetString(bytes);

                bytes = new byte[4];
                stream.Read(bytes, 0, 4);
                Version = BitConverter.ToInt32(bytes, 0);

                stream.Read(bytes, 0, 4);
                ManifestOffset = BitConverter.ToInt32(bytes, 0);

                stream.Seek(ManifestOffset, SeekOrigin.Begin);
                stream.Read(bytes, 0, 4);
                Manifest = new Manifest
                {
                    NumberOfFiles = BitConverter.ToInt32(bytes, 0),
                    ManifestEntries = new List<ManifestEntry>()
                };

                for (int i = 0; i < Manifest.NumberOfFiles; i++)
                {
                    var manifestEntry = ParseManifestEntry(stream);
                    Manifest.ManifestEntries.Add(manifestEntry);
                }
            }
        }

        private ManifestEntry ParseManifestEntry(Stream stream)
        {
            var bytes = new byte[4];
            var manifestEntry = new ManifestEntry();

            stream.Read(bytes, 0, 4);
            manifestEntry.FileLength1 = BitConverter.ToInt32(bytes, 0);

            stream.Read(bytes, 0, 4);
            manifestEntry.FileLength2 = BitConverter.ToInt32(bytes, 0);

            stream.Read(bytes, 0, 4);
            manifestEntry.FileOffset = BitConverter.ToInt32(bytes, 0);

            stream.Read(bytes, 0, 4);
            manifestEntry.FilenameLength = BitConverter.ToInt32(bytes, 0);

            bytes = new byte[manifestEntry.FilenameLength];
            stream.Read(bytes, 0, manifestEntry.FilenameLength);
            manifestEntry.Filename = Encoding.UTF8.GetString(bytes);

            return manifestEntry;
        }

        /// <summary>
        /// Return a byte array for a file name in the far. The file name must be exact.
        /// </summary>
        /// <param name="filename">The name of the file in the far.</param>
        /// <returns>A byte array of the content in the far.</returns>
        public byte[] GetBytes(string filename)
        {
            var entry = Manifest.ManifestEntries.FirstOrDefault(m => m.Filename == filename);
            if (entry == null)
                throw new ManifestEntryNotFoundException($"The entry could not be found, {filename}.");
            return GetBytes(entry);
        }

        /// <summary>
        /// Try to return a byte array for a file name in the fire. The file name must be exact.
        /// </summary>
        /// <param name="filename">The name of the file in the far</param>
        /// <param name="bytes">A byte array to receive the bytes</param>
        /// <returns>Whether or not the file was found in the .far</returns>
        public bool TryGetBytes(string filename, out byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                bytes = null;
                return false;
            }
            var entry = Manifest.ManifestEntries.FirstOrDefault(m => m.Filename == filename);
            if (entry is null)
            {
                bytes = null;
                return false;
            }

            bytes = GetBytes(entry);
            return true;
        }

        /// <summary>
        /// Return a byte array for the given Manifest entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>A byte array of the content in the far.</returns>
        public byte[] GetBytes(ManifestEntry entry)
        {
            var bytes = new byte[entry.FileLength1];
            using (var stream = new FileStream(PathToFar, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(entry.FileOffset, SeekOrigin.Begin);
                stream.Read(bytes, 0, entry.FileLength1);
                return bytes;
            }
        }

        /// <summary>
        /// Extract a file by its manifest entry from the far.
        /// </summary>
        /// <param name="entry">The manifest entry to extract.</param>
        /// <param name="outputDirectory">The directory to extract the files to. Otherwise it will extract them at the current directory.</param>
        /// <param name="preserveDirectories">Whether or not to create the directories from a filename. If a filename was Community\Bus_loadscreen_800x600.bmp, Community\ would or wouldn't be created depending on this parameter. true = create it, false = strip it.</param>
        public void Extract(ManifestEntry entry, string outputDirectory = "", bool preserveDirectories = true)
        {
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            using (var stream = new FileStream(PathToFar, FileMode.Open, FileAccess.Read))
            {
                ExtractEntry(stream, entry, outputDirectory, preserveDirectories);
            }
        }

        /// <summary>
        /// Extract a file(s) by its file name in the far. If the the Manifest entry file name contains a directory, you must include that exactly.
        /// </summary>
        /// <param name="fileName">The filename of the Manifest entry.</param>
        /// <param name="outputDirectory">The directory to extract the files to. Otherwise it will extract them at the current directory.</param>
        /// <param name="preserveDirectories">Whether or not to create the directories from a filename. If a filename was Community\Bus_loadscreen_800x600.bmp, Community\ would or wouldn't be created depending on this parameter. true = create it, false = strip it.</param>
        public void Extract(string fileName, string outputDirectory = "", bool preserveDirectories = true)
        {
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            using (var stream = new FileStream(PathToFar, FileMode.Open, FileAccess.Read))
            {
                foreach (var entry in Manifest.ManifestEntries.Where(m => m.Filename == fileName))
                {
                    ExtractEntry(stream, entry, outputDirectory, preserveDirectories);
                }
            }
        }

        private void ExtractEntry(Stream stream, ManifestEntry entry, string outputDirectory, bool preserveDirectories)
        {
            if (outputDirectory.Contains(Path.DirectorySeparatorChar))
                Directory.CreateDirectory(Path.Combine(outputDirectory, Path.GetDirectoryName(entry.Filename) ?? string.Empty));
            var bytes = new byte[entry.FileLength1];
            stream.Seek(entry.FileOffset, SeekOrigin.Begin);
            stream.Read(bytes, 0, entry.FileLength1);
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
