namespace Sims.Far
{
    public interface IFar
    {
        /// <summary>
        /// The manifest contains a count of the number of archived files, followed by an entry for each file. In all of the examples examined the order of the entries matches the order of the archived files, but whether this is a firm requirement or not is unknown.
        /// </summary>
        Manifest Manifest { get; set; }
        /// <summary>
        /// The manifest offset is the byte offset from the beginning of the file to the manifest.
        /// The contents of the archived files are simply concatenated together without any other structure or padding.Caveat: all of the files observed have been a multiple of four in length, so it's possible that the files may be padded to a two-byte or four-byte boundary and the case has simply never been encountered.
        /// </summary>
        int ManifestOffset { get; set; }
        /// <summary>
        /// The signature is an eight-byte string, consisting literally of "FAR!byAZ" (without the quotes).
        /// </summary>
        string Signature { get; set; }
        /// <summary>
        /// The manifest offset is the byte offset from the beginning of the file to the manifest.
        /// The contents of the archived files are simply concatenated together without any other structure or padding.Caveat: all of the files observed have been a multiple of four in length, so it's possible that the files may be padded to a two-byte or four-byte boundary and the case has simply never been encountered.
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// Path to the far file to work with.
        /// </summary>
        string PathToFar { get; set; }

        /// <summary>
        /// Extract a file by its manifest entry from the far.
        /// </summary>
        /// <param name="entry">The manifest entry to extract.</param>
        /// <param name="outputDirectory">The directory to extract the files to. Otherwise it will extract them at the current directory.</param>
        /// <param name="preserveDirectories">Whether or not to create the directories from a filename. If a filename was Community\Bus_loadscreen_800x600.bmp, Community\ would or wouldn't be created depending on this parameter. true = create it, false = strip it.</param>
        void Extract(ManifestEntry entry, string outputDirectory = "", bool preserveDirectories = true);
        /// <summary>
        /// Extract a file(s) by its file name in the far. If the the Manifest entry file name contains a directory, you must include that exactly.
        /// </summary>
        /// <param name="fileName">The filename of the Manifest entry.</param>
        /// <param name="outputDirectory">The directory to extract the files to. Otherwise it will extract them at the current directory.</param>
        /// <param name="preserveDirectories">Whether or not to create the directories from a filename. If a filename was Community\Bus_loadscreen_800x600.bmp, Community\ would or wouldn't be created depending on this parameter. true = create it, false = strip it.</param>
        void Extract(string fileName, string outputDirectory = "", bool preserveDirectories = true);
        /// <summary>
        /// Return a byte array for a file name in the far. The file name must be exact.
        /// </summary>
        /// <param name="filename">The name of the file in the far.</param>
        /// <returns>A byte array of the content in the far.</returns>
        byte[] GetBytes(string filename);
        /// <summary>
        /// Return a byte array for the given Manifest entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>A byte array of the content in the far.</returns>
        byte[] GetBytes(ManifestEntry entry);

        /// <summary>
        /// Try to return a byte array for a file name in the fire. The file name must be exact.
        /// </summary>
        /// <param name="filename">The name of the file in the far</param>
        /// <param name="bytes">A byte array to receive the bytes</param>
        /// <returns>Whether or not the file was found in the .far</returns>
        bool TryGetBytes(string filename, out byte[] bytes);

        /// <summary>
        /// Parse the far file
        /// </summary>
        void ParseFar();
    }
}