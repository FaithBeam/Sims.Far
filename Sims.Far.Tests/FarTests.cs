using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Sims.Far.Tests
{
    [TestClass]
    public class FarTests
    {
        [TestMethod]
        public void TestParseFar()
        {
            var far = new Far("test.far");
            far.ParseFar();

            Assert.AreEqual(far.Signature, "FAR!byAZ");
            Assert.AreEqual(far.Version, 1);
            Assert.AreEqual(far.ManifestOffset, 160);
            Assert.AreEqual(far.Manifest.NumberOfFiles, 1);
            Assert.AreEqual(far.Manifest.ManifestEntries.Count, 1);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FileLength1, 144);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FileLength2, 144);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].Filename, "test.bmp");
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FilenameLength, 8);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FileOffset, 16);
        }

        [TestMethod]
        public void TestExtract()
        {
            var far = new Far("test.far");
            far.ParseFar();
            far.Extract();
            Assert.IsTrue(File.Exists("test.bmp"));

            // Assert the extracted file matches the file in the .far exactly
            using (var outFs = new FileStream("test.bmp", FileMode.Open, FileAccess.Read))
            using (var farFs = new FileStream("test.far", FileMode.Open, FileAccess.Read))
            {
                farFs.Seek(16, SeekOrigin.Begin);
                for (int i = 0; i < 144; i++)
                    Assert.AreEqual(farFs.ReadByte(), outFs.ReadByte());                    
            }

            File.Delete("test.bmp");
            far.Extract(outputDirectory: "UIGraphics");
            Assert.IsTrue(File.Exists(Path.Combine("UIGraphics", "test.bmp")));
            Directory.Delete("UIGraphics", true);
        }
    }
}
