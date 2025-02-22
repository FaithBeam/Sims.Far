﻿using System.Text;

namespace Sims.Far.Tests;

public class Tests
{
    [Test]
    public void Read_far()
    {
        var far = Far.Read(FarFilePath);
    }

    [Test]
    public async Task Write_far()
    {
        var far = Far.Read(FarFilePath);
        far.Write("newfar.far");
        var oldFarBytes = await File.ReadAllBytesAsync(FarFilePath);
        var newFarBytes = await File.ReadAllBytesAsync("newfar.far");
        await Assert.That(oldFarBytes).IsEquivalentTo(newFarBytes);
    }

    [Test]
    public void Extract_files()
    {
        var tmpDir = "tmpDir";
        if (!Directory.Exists(tmpDir))
        {
            Directory.CreateDirectory(tmpDir);
        }
        var far = Far.Read(FarFilePath);
        foreach (var ff in far.Files)
        {
            var directories = Path.GetDirectoryName(ff.Name);
            if (!string.IsNullOrWhiteSpace(directories))
            {
                var combined = Path.Join(tmpDir, directories);
                if (!Directory.Exists(combined))
                {
                    Directory.CreateDirectory(combined);
                }
            }
            var outPath = Path.Join(tmpDir, ff.Name);
            ff.Extract(outPath);
        }
    }

    [Test]
    public void Create_new_far()
    {
        var files = new List<FarFile> { new("Some file name", "Some content"u8.ToArray()) };
        var far = new Far(files);
        far.Write("newfar.far");
    }

    private const string FarFilePath =
        @"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far";
}
