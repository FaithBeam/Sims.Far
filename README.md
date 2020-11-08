# Sims.Far
A library to manipulate The Sims 1 .far files.

## Installation

You can install Sims.Far as a nupkg from nuget.org.

## Usage

Extracting UIGraphics.far to a relative UIGraphics folder:
```cs
using Sims.Far;

void main()
{
  var far = new Far(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
  far.Extract(@"UIGraphics\");
}
```

Extracting UIGraphics.far:
```cs
using Sims.Far;

void main()
{
  var far = new Far(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
  far.Extract();
}
```

Extracting UIGraphics.far with an inclusive filter:
```cs
using Sims.Far;

void main()
{
  var myFiles = new List<string> { "Res_CPanel.h", @"Community\Bus_loadscreen_800x600.bmp" };
  var far = new Far(myFiles);
  far.Extract();
}
```

Extracting UIGraphics.far with an inclusive filter to a specified directory:
```cs
using Sims.Far;

void main()
{
  var myFiles = new List<string> { "Res_CPanel.h", @"Community\Bus_loadscreen_800x600.bmp" };
  var far = new Far("UIGraphics", myFiles);
  far.Extract();
}
```

## Credits

[http://simtech.sourceforge.net/tech/far.html](http://simtech.sourceforge.net/tech/far.html)
