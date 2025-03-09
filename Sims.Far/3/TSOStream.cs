using System.IO;

namespace Sims.Far._3;

public static class TsoStream
{
    public static byte[] Read(BinaryReader br, bool compressed, int decompressedFileSize)
    {
        if (compressed)
        {
            br.BaseStream.Seek(9, SeekOrigin.Current);
            var fileSize = br.ReadUInt32();
            var compressionId = br.ReadUInt16();
            if (compressionId == 0xFB10)
            {
                var dummy = br.ReadBytes(3);
                var decompressedSize = (uint)((dummy[0] << 0x10) | (dummy[1] << 0x08) | +dummy[2]);
                var dec = new Decompresser
                {
                    CompressedSize = fileSize,
                    DecompressedSize = decompressedSize,
                };
                var data = dec.Decompress(br.ReadBytes((int)fileSize));
                return data;
            }
            else { }
        }
        else
        {
            return br.ReadBytes(decompressedFileSize);
        }
        throw new InvalidDataException();
    }

    public static void Write(BinaryWriter bw, bool compress, byte[] data)
    {
        if (compress)
        {
            // bw.Write([0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
            var dec = new Decompresser();
            var compressed = dec.Compress(data);
            bw.Write(compressed);
        }
        else
        {
            bw.Write(data);
        }
    }
}
