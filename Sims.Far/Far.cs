using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sims.Far;

//           FAR File Format
// +--------+--------+------------------+
// | Offset |  Size  |      Value       |
// +--------+--------+------------------+
// |   0    |   8    |    Signature     |
// |   8    |   4    |     Version      |
// |   12   |   4    | Manifest offset  |
// |   16   |  var   |     File 1       |
// |  var   |  var   |     File 2       |
// |  var   |  var   |     . . .        |
// |  var   |  var   |     File N       |
// |  var   |  var   |    Manifest      |
// +--------+--------+------------------+
public class Far(List<FarFile> farFiles)
{
    private static string Signature => "FAR!byAZ";
    private static int Version => 1;
    public List<FarFile> Files { get; set; } = farFiles;

    /// <summary>
    /// Create a .far file at a path
    /// </summary>
    /// <param name="path"></param>
    public void Write(string path)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs, Encoding.UTF8);
        bw.Write(Signature.Select(x => x).ToArray());
        bw.Write(Version);

        // save manifestoffset position to be written later
        var manifestOffsetPos = (int)bw.BaseStream.Position;
        bw.BaseStream.Position += 4;

        // write files and calculate manifest entries
        var manifestEntries = new List<ManifestEntry>();
        foreach (var ff in Files)
        {
            manifestEntries.Add(
                new ManifestEntry(
                    ff.Bytes.Length,
                    ff.Bytes.Length,
                    (int)bw.BaseStream.Position,
                    ff.Name
                )
            );
            ff.Write(bw);
        }

        // write manifestoffset
        var manifestPos = (int)bw.BaseStream.Position;
        bw.BaseStream.Seek(manifestOffsetPos, SeekOrigin.Begin);
        bw.Write(manifestPos);

        // calculate manifest and write manifest
        bw.BaseStream.Seek(manifestPos, SeekOrigin.Begin);
        var manifest = new Manifest(Files.Count, manifestEntries);
        manifest.Write(bw);
    }

    public static Far Read(string pathToFar)
    {
        using var fs = File.OpenRead(pathToFar);
        var br = new BinaryReader(fs, Encoding.UTF8);
        // skip to manifest offset because signature and version are always the same
        br.BaseStream.Position = 12;
        var manifestOffset = br.ReadInt32();
        br.BaseStream.Seek(manifestOffset, SeekOrigin.Begin);
        var manifest = Manifest.Read(br);
        var files = manifest
            .Entries.Select(x =>
            {
                fs.Seek(x.FileOffset, SeekOrigin.Begin);
                return new FarFile(x.Filename, br.ReadBytes(x.FileLength));
            })
            .ToList();
        return new Far(files);
    }
}

public class FarFile(string name, byte[] bytes)
{
    /// <summary>
    /// Offset from the beginning of the file
    /// </summary>
    public string Name { get; } = name;
    public byte[] Bytes { get; } = bytes;

    public void Extract(string path) => File.WriteAllBytes(path, Bytes);

    internal void Write(BinaryWriter stream) => stream.Write(Bytes);
}

//                    Manifest
// +--------+--------+----------------------------+
// | Offset |  Size  |          Value             |
// +--------+--------+----------------------------+
// |   0    |   4    |    Number of Files (N)     |
// |   4    |  var   |     Manifest entry 1       |
// |  var   |  var   |     Manifest entry 2       |
// |  var   |  var   |         . . .              |
// |  var   |  var   |     Manifest entry N       |
// +--------+--------+----------------------------+
internal class Manifest
{
    private int NumberOfFiles { get; }
    internal List<ManifestEntry> Entries { get; }

    internal Manifest(int numberOfFiles, List<ManifestEntry> entries)
    {
        NumberOfFiles = numberOfFiles;
        Entries = entries;
    }

    internal void Write(BinaryWriter stream)
    {
        stream.Write(NumberOfFiles);
        foreach (var me in Entries)
        {
            me.Write(stream);
        }
    }

    internal static Manifest Read(BinaryReader br)
    {
        var numberOfFiles = br.ReadInt32();
        var entries = new List<ManifestEntry>();
        for (var i = 0; i < numberOfFiles; i++)
        {
            entries.Add(ManifestEntry.Read(br));
        }
        return new Manifest(numberOfFiles, entries);
    }
}

//              Manifest Entry
// +--------+--------+--------------------+
// | Offset |  Size  |    Value           |
// +--------+--------+--------------------+
// |   0    |   4    |    File length     |
// |   4    |   4    |    File length     |
// |   8    |   4    |    File offset     |
// |  12    |   4    |    Filename length |
// |  16    |  var   |    Filename        |
// +--------+--------+--------------------+
internal class ManifestEntry
{
    internal int FileLength { get; }
    private int FileLength2 { get; }
    internal int FileOffset { get; }
    private int FileNameLength => Filename.Length;
    internal string Filename { get; }

    internal ManifestEntry(int fileLength, int fileLength2, int fileOffset, string fileName)
    {
        FileLength = fileLength;
        FileLength2 = fileLength2;
        FileOffset = fileOffset;
        Filename = fileName;
    }

    internal void Write(BinaryWriter stream)
    {
        stream.Write(FileLength);
        stream.Write(FileLength2);
        stream.Write(FileOffset);
        stream.Write(FileNameLength);
        stream.Write(Filename.Select(x => x).ToArray());
    }

    internal static ManifestEntry Read(BinaryReader br)
    {
        var fileLength = br.ReadInt32();
        var fileLength2 = br.ReadInt32();
        var fileOffset = br.ReadInt32();
        var fileNameLength = br.ReadInt32();
        var fileName = string.Concat(br.ReadChars(fileNameLength));
        return new ManifestEntry(fileLength, fileLength2, fileOffset, fileName);
    }
}
