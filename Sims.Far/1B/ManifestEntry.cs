using System.IO;
using System.Linq;

namespace Sims.Far._1B;

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
        stream.Write((short)FileNameLength);
        stream.Write(Filename.Select(x => x).ToArray());
    }

    internal static ManifestEntry Read(BinaryReader br)
    {
        var fileLength = br.ReadInt32();
        var fileLength2 = br.ReadInt32();
        var fileOffset = br.ReadInt32();
        var fileNameLength = br.ReadInt16();
        var fileName = string.Concat(br.ReadChars(fileNameLength));
        return new ManifestEntry(fileLength, fileLength2, fileOffset, fileName);
    }
}
