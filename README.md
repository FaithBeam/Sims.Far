# Sims.Far

A library to manipulate The Sims 1 .far files.

## Installation

You can install Sims.Far as a nupkg from nuget.org.

## Usage

Extract Studiotown\largeback.bmp from UIGraphics.far using a file name:

```cs
using Sims.Far;

void main()
{
    var far = new Far(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
    far.Extract("Studiotown\largeback.bmp");
}
```

Extract Studiotown\largeback.bmp from UIGraphics.far using a manifest entry:

```cs
using Sims.Far;
using System.Linq;

void main()
{
    var far = new Far(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
    var entry = far.Manifest.ManifestEntries.FirstOrDefault(m => m.Filename == "Studiotown\largeback.bmp");
    far.Extract(entry);
}
```

Extract everything from UIGraphics.far:

```cs
using Sims.Far;

void main()
{
    var far = new Far(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
    foreach (var entry in far.Manifest.ManifestEntries)
        far.Extract(entry);
}
```

## Credits

[http://simtech.sourceforge.net/tech/far.html](http://simtech.sourceforge.net/tech/far.html)
