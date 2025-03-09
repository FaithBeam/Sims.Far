using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sims.Far._3;

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
public class Far(FarVersion version, List<FarFile> farFiles)
{
    private static string Signature => "FAR!byAZ";
    public FarVersion Version { get; set; } = version;
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
        switch (Version)
        {
            case FarVersion._1A:
            case FarVersion._1B:
                bw.Write(1);
                break;
            case FarVersion._3:
                throw new SimsFarException("Cannot write v3 far files.");
            default:
                throw new ArgumentOutOfRangeException();
        }

        // save manifestoffset position to be written later
        var manifestOffsetPos = (int)bw.BaseStream.Position;
        bw.BaseStream.Position += 4;

        // write files and calculate manifest entries
        var manifestEntries = new List<BaseManifestEntry>();
        foreach (var ff in Files)
        {
            switch (Version)
            {
                case FarVersion._1A:
                    manifestEntries.Add(
                        new _1A.ManifestEntry(
                            ff.Bytes.Length,
                            ff.Bytes.Length,
                            (int)bw.BaseStream.Position,
                            ff.Name
                        )
                    );
                    ff.Write(bw);
                    break;
                case FarVersion._1B:
                    manifestEntries.Add(
                        new _1B.ManifestEntry(
                            ff.Bytes.Length,
                            ff.Bytes.Length,
                            (int)bw.BaseStream.Position,
                            ff.Name
                        )
                    );
                    ff.Write(bw);
                    break;
                case FarVersion._3:
                    // new _3.ManifestEntry(ff.Bytes.Length, ff.Bytes.Length)
                    ff.Write(bw, isV3: true, compress: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
        // skip to version offset because signature is always the same
        br.BaseStream.Position = 8;
        var version = br.ReadInt32() switch
        {
            1 => FarVersion._1A,
            3 => FarVersion._3,
            _ => throw new ArgumentOutOfRangeException(),
        };
        var manifestOffset = br.ReadInt32();
        br.BaseStream.Seek(manifestOffset, SeekOrigin.Begin);
        var manifest = Manifest.Read(version, br);
        var files = manifest
            .Entries.Select(x =>
            {
                fs.Seek(x.FileOffset, SeekOrigin.Begin);
                if (x is _3.ManifestEntry v3Me)
                {
                    return new FarFile(
                        x.Filename,
                        TsoStream.Read(br, v3Me.Compressed, v3Me.DecompressedFileSize)
                    );
                }

                return new FarFile(x.Filename, br.ReadBytes(x.CompressedFileSize));
            })
            .ToList();

        // correct the far version to 1B
        if (version == FarVersion._1A && manifest.Entries.Any(x => x is _1B.ManifestEntry))
        {
            version = FarVersion._1B;
        }
        return new Far(version, files);
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

    internal void Write(BinaryWriter bw, bool isV3 = false, bool compress = false)
    {
        if (isV3)
        {
            TsoStream.Write(bw, compress, Bytes);
            // bw.Write(Bytes);
        }
        else
        {
            bw.Write(Bytes);
        }
    }
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
    internal List<BaseManifestEntry> Entries { get; }

    internal Manifest(int numberOfFiles, List<BaseManifestEntry> entries)
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

    internal static Manifest Read(FarVersion farVersion, BinaryReader br)
    {
        var numberOfFiles = br.ReadInt32();
        var entries = new List<BaseManifestEntry>();
        for (var i = 0; i < numberOfFiles; i++)
        {
            entries.Add(ManifestEntryReader.Read(farVersion, br));
        }
        return new Manifest(numberOfFiles, entries);
    }
}

internal static class ManifestEntryReader
{
    internal static BaseManifestEntry Read(FarVersion farVersion, BinaryReader br) =>
        farVersion switch
        {
            FarVersion._1A when IsFar1B(br) => _1B.ManifestEntry.Read(br),
            FarVersion._1A => _1A.ManifestEntry.Read(br),
            FarVersion._1B => _1B.ManifestEntry.Read(br),
            FarVersion._3 => _3.ManifestEntry.Read(br),
            _ => throw new ArgumentOutOfRangeException(nameof(farVersion)),
        };

    /// <summary>
    /// Determine if this version 1 Far is B
    /// </summary>
    /// <param name="br"></param>
    /// <returns></returns>
    private static bool IsFar1B(BinaryReader br)
    {
        var originalPosition = br.BaseStream.Position;
        br.BaseStream.Position += 14;
        var isB = br.ReadInt16() > 0;
        br.BaseStream.Position = originalPosition;
        return isB;
    }
}

internal abstract class BaseManifestEntry
{
    internal int DecompressedFileSize { get; }
    internal int CompressedFileSize { get; }
    internal int FileOffset { get; }
    internal int FileNameLength => Filename.Length;
    internal string Filename { get; }

    internal BaseManifestEntry(
        int decompressedFileSize,
        int compressedFileSize,
        int fileOffset,
        string fileName
    )
    {
        DecompressedFileSize = decompressedFileSize;
        CompressedFileSize = compressedFileSize;
        FileOffset = fileOffset;
        Filename = fileName;
    }

    internal abstract void Write(BinaryWriter stream);
}
