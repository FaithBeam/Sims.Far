# Sims.Far

A library to extract and create The Sims 1 .far files.

## Installation

You can install Sims.Far as a nupkg from nuget.org.

This library targets netstandard2.0 so you need .NET Framework >= 4.6.2 OR .NET/.NET Core >= 2.0.

## Usage

Extract everything from UIGraphics.far:

```cs
using Sims.Far;

static void Main(string[] args)
{
    var far = Far.Read(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
    var outputFolder = "somedir";
    foreach (var file in far.Files)
    {
        var directories = Path.GetDirectoryName(file.Name);
        if (!string.IsNullOrWhiteSpace(directories))
        {
            var combined = Path.Join(outputFolder, directories);
            if (!Directory.Exists(combined))
            {
                Directory.CreateDirectory(combined);
            }
        }
        var outPath = Path.Join(outputFolder, file.Name);
        file.Extract(outPath);
    }
}
```

Create a new far file with some content and write it to newfar.far:

```csharp
using Sims.Far;

static void Main(string[] args)
{
    var files = new List<FarFile> { new("Some file name", "Some content"u8.ToArray()) };
    var far = new Far(files);
    far.Write("newfar.far");
}
```

## Development

Install the latest [Visual Studio Community Edition](https://visualstudio.microsoft.com/vs/community/) or [Jetbrains Rider](https://www.jetbrains.com/rider/) (both are free).
If for some reason the latest .NET SDK isn't installed with Visual Studio or Rider, install the latest [.NET SDK](https://dotnet.microsoft.com/en-us/download).

```bash
git clone https://github.com/FaithBeam/Sims.Far
cd Sims.Far
Open Sims.Far.sln with Visual Studio or Rider
```

## Credits

[https://web.archive.org/web/20220410061532/https://simtech.sourceforge.net/tech/far.html](https://web.archive.org/web/20220410061532/https://simtech.sourceforge.net/tech/far.html)
