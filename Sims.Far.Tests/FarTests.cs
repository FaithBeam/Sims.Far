using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace Sims.Far.Tests
{
    [TestClass]
    public class FarTests
    {
        private readonly string _farFile = "test.far";
        private readonly string _fileName = "test.bmp";

        [TestMethod]
        public void TestParseFar()
        {
            var far = new Far(_farFile);
            Assert.AreEqual(far.Signature, "FAR!byAZ");
            Assert.AreEqual(far.Version, 1);
            Assert.AreEqual(far.ManifestOffset, 160);
            Assert.AreEqual(far.Manifest.NumberOfFiles, 1);
            Assert.AreEqual(far.Manifest.ManifestEntries.Count, 1);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FileLength1, 144);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FileLength2, 144);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].Filename, _fileName);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FilenameLength, 8);
            Assert.AreEqual(far.Manifest.ManifestEntries[0].FileOffset, 16);
        }


        [TestMethod]
        public void TestGetBytesByFileName()
        {
            var far = new Far(_farFile);
            far.Extract(_fileName);
            var bytes = far.GetBytes(_fileName);
            using (var farFs = new FileStream(_farFile, FileMode.Open, FileAccess.Read))
            {
                farFs.Seek(16, SeekOrigin.Begin);
                for (int i = 0; i < 144; i++)
                    Assert.AreEqual(farFs.ReadByte(), bytes[i]);
            }
        }

        [TestMethod]
        public void TestGetBytesByManifestEntry()
        {
            var far = new Far(_farFile);
            far.Extract(_fileName);
            var bytes = far.GetBytes(far.Manifest.ManifestEntries.FirstOrDefault(m => m.Filename == _fileName));
            using (var farFs = new FileStream(_farFile, FileMode.Open, FileAccess.Read))
            {
                farFs.Seek(16, SeekOrigin.Begin);
                for (int i = 0; i < 144; i++)
                    Assert.AreEqual(farFs.ReadByte(), bytes[i]);
            }
        }

        [TestMethod]
        public void TestExtractByManifestEntry()
        {
            var far = new Far(_farFile);
            var entry = far.Manifest.ManifestEntries.FirstOrDefault(m => m.Filename == _fileName);
            far.Extract(entry);
            Assert.IsTrue(File.Exists(entry.Filename));

            // Assert the extracted file matches the file in the .far exactly
            using (var outFs = new FileStream(entry.Filename, FileMode.Open, FileAccess.Read))
            using (var farFs = new FileStream(_farFile, FileMode.Open, FileAccess.Read))
            {
                farFs.Seek(16, SeekOrigin.Begin);
                for (int i = 0; i < 144; i++)
                    Assert.AreEqual(farFs.ReadByte(), outFs.ReadByte());
            }

            File.Delete(entry.Filename);
        }

        [TestMethod]
        public void TestExtractByFileName()
        {
            var far = new Far(_farFile);
            far.Extract(_fileName);
            Assert.IsTrue(File.Exists(_fileName));

            // Assert the extracted file matches the file in the .far exactly
            using (var outFs = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            using (var farFs = new FileStream(_farFile, FileMode.Open, FileAccess.Read))
            {
                farFs.Seek(16, SeekOrigin.Begin);
                for (int i = 0; i < 144; i++)
                    Assert.AreEqual(farFs.ReadByte(), outFs.ReadByte());
            }

            File.Delete(_fileName);
        }
    }
}
