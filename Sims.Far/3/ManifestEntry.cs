using System.IO;
using System.Linq;

namespace Sims.Far._3;

internal class ManifestEntry(
    int decompressedFileSize,
    int compressedFileSize,
    byte dataType,
    int fileOffset,
    string fileName,
    byte accessNumber,
    int typeId,
    int fileId,
    bool compressed
) : BaseManifestEntry(decompressedFileSize, compressedFileSize, fileOffset, fileName)
{
    internal byte AccessNumber { get; } = accessNumber;
    internal int TypeId { get; } = typeId;
    internal int FileId { get; } = fileId;
    internal byte DataType { get; } = dataType;
    internal bool Compressed { get; } = compressed;

    internal static ManifestEntry Read(BinaryReader br)
    {
        var decompressedFileSize = br.ReadInt32();
        var dummy = br.ReadBytes(3);
        var compressedFileSize = (uint)((dummy[0] << 0) | (dummy[1] << 8) | (dummy[2]) << 16);
        var dataType = br.ReadByte();
        var dataOffset = br.ReadInt32();
        var compressed = br.ReadBoolean();
        var accessNumber = br.ReadByte();
        var fileNameLength = br.ReadInt16();
        var typeId = br.ReadInt32();
        var fileId = br.ReadInt32();
        var fileName = string.Concat(br.ReadChars(fileNameLength));
        return new ManifestEntry(
            decompressedFileSize,
            (int)compressedFileSize,
            dataType,
            dataOffset,
            fileName,
            accessNumber,
            typeId,
            fileId,
            compressed
        );
    }

    internal override void Write(BinaryWriter bw)
    {
        bw.Write(DecompressedFileSize);

        var dummy = new byte[3];
        dummy[0] = (byte)(DecompressedFileSize & 0xFF);
        dummy[1] = (byte)((DecompressedFileSize >> 8) & 0xFF);
        dummy[2] = (byte)((DecompressedFileSize >> 16) & 0xFF);

        bw.Write(dummy);

        bw.Write(DataType);

        bw.Write(FileOffset);

        // always write false for compressed
        bw.Write((byte)0x00);

        bw.Write(AccessNumber);

        bw.Write((short)Filename.Length);

        bw.Write(TypeId);

        bw.Write(FileId);

        bw.Write(Filename.Select(x => x).ToArray());
    }
}
