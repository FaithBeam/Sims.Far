using System.IO;
using System.Linq;

namespace Sims.Far._1A;

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
internal class ManifestEntry(
    int decompressedFileSize,
    int compressedFileSize,
    int fileOffset,
    string fileName
) : BaseManifestEntry(decompressedFileSize, compressedFileSize, fileOffset, fileName)
{
    internal override void Write(BinaryWriter stream)
    {
        stream.Write(DecompressedFileSize);
        stream.Write(CompressedFileSize);
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
