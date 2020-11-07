using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sims.Far
{
    public class Far
    {
        public readonly string far;
        public List<Bmp> bmps = new List<Bmp>();
        public List<string> paths = new List<string>();
        public byte[] bytes;

        public Far(string far)
        {
            this.far = far;
            ParseFar();
        }

        private void ParseFar()
        {
            bytes = File.ReadAllBytes(this.far);
            int curByte = bytes[0];
            int nextByte = bytes[1];
            int sizeOfBITMAPINFOHEADER;
            int sizeOfBmpBytes;
            int endBmp = 0;
            int i = 0;
            for (; i < bytes.Length - 2; i++)
            {
                // Start of BMP header
                if (curByte == 66 && nextByte == 77)
                {
                    sizeOfBITMAPINFOHEADER = bytes[i - 1 + 14];
                    // Header validation. BITMAPINFOHEADER will always be 40 so if it isn't, these bytes are not the start of a bmp.
                    if (sizeOfBITMAPINFOHEADER == 40)
                    {
                        sizeOfBmpBytes = BitConverter.ToInt32(new byte[] { bytes[i + 1], bytes[i + 2], bytes[i + 3], bytes[i + 4] }, 0);
                        endBmp = i - 1 + sizeOfBmpBytes;
                        this.bmps.Add(new Bmp { StartOffset = i - 1, EndOffset = endBmp });
                        i = endBmp;
                    }
                }
                curByte = bytes[i];
                nextByte = bytes[i + 1];
            }
            i = endBmp;

            // Bytes that represent acceptable ascii characters to create paths
            var asciiChars = new List<int> { 45, 46, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 92, 95, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122 };
            // Navigate to file names
            while (!asciiChars.Contains(bytes[i]) && i < bytes.Length) { i++; }

            // Extensions that are found in the .far file. Currently only works with UIGraphics.far.
            var extensions = new List<string> { ".h", ".rt", ".bmp", ".tga", ".cur" };
            var sb = new StringBuilder();
            // Start at the end of all bmps
            for (; i < bytes.Length; i++)
            {
                curByte = bytes[i];
                if (asciiChars.Contains(curByte))
                {
                    sb.Append(Convert.ToChar(curByte));
                }
                else
                {
                    // Skip strings that couldn't possible be a file
                    if (sb.Length > 5)
                    {
                        string path = "";
                        var tmpPath = sb.ToString();
                        // tmppath may still have dangling characters following the extension: .bmp8, .bmpR, etc. This replaces the tmppath's extension entirely.
                        foreach (var ext in extensions)
                        {
                            if (tmpPath.IndexOf(ext, StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                path = tmpPath.Substring(0, tmpPath.IndexOf(".")) + ext;
                                break;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(path) && (path.IndexOf(".bmp", StringComparison.OrdinalIgnoreCase) > 0))
                            paths.Add(path);
                    }
                    sb.Clear();
                }
            }
            // This feels bad
            paths.Add(sb.ToString());
            sb.Clear();
        }

        public void Extract()
        {
            for (int j = 0; j < this.paths.Count; j++)
            {
                this.bmps[j].Path = this.paths[j];
                int size = this.bmps[j].EndOffset - this.bmps[j].StartOffset;
                var curBmp = new byte[size];
                if (this.paths[j].Contains(@"\"))
                    Directory.CreateDirectory(@".\" + this.paths[j].Substring(0, this.paths[j].LastIndexOf(@"\")));
                Array.Copy(this.bytes, this.bmps[j].StartOffset, curBmp, 0, size);
                File.WriteAllBytes(@".\" + this.bmps[j].Path, curBmp);
            }
        }

        public void Extract(string outputDirectory)
        {
            if (!outputDirectory.EndsWith(@"\")) outputDirectory += @"\";
            for (int j = 0; j < this.paths.Count; j++)
            {
                this.bmps[j].Path = this.paths[j];
                int size = this.bmps[j].EndOffset - this.bmps[j].StartOffset;
                var curBmp = new byte[size];
                if (this.paths[j].Contains(@"\"))
                    Directory.CreateDirectory(outputDirectory + this.paths[j].Substring(0, this.paths[j].LastIndexOf(@"\")));
                Array.Copy(this.bytes, this.bmps[j].StartOffset, curBmp, 0, size);
                File.WriteAllBytes(outputDirectory + this.bmps[j].Path, curBmp);
            }
        }

        public void Extract(string outputDirectory, IEnumerable<string> filesToExtract)
        {
            if (!outputDirectory.EndsWith(@"\")) outputDirectory += @"\";
            for (int j = 0; j < this.paths.Count; j++)
            {
                if (filesToExtract.Contains(this.paths[j], StringComparer.OrdinalIgnoreCase))
                {
                    this.bmps[j].Path = this.paths[j];
                    int size = this.bmps[j].EndOffset - this.bmps[j].StartOffset;
                    var curBmp = new byte[size];
                    if (this.paths[j].Contains(@"\"))
                        Directory.CreateDirectory(outputDirectory + this.paths[j].Substring(0, this.paths[j].LastIndexOf(@"\")));
                    Array.Copy(this.bytes, this.bmps[j].StartOffset, curBmp, 0, size);
                    File.WriteAllBytes(outputDirectory + this.bmps[j].Path, curBmp);
                }
            }
        }
    }
}
