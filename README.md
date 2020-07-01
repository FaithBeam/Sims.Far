**This is currently considered pre-release**

# Sims.Far
A library to manipulate The Sims 1 .far files.

Currently only extracts .bmps from UIGraphics.far, and has only been used on The Sims 1 Complete Collection UIGraphics.far with CRC32: 8E03701F.

## Usage

```cs
using Sims.Far;

void main()
{
  var far = new Far(@"C:\Program Files (x86)\Maxis\The Sims\UIGraphics\UIGraphics.far");
  far.Extract(@"UIGraphics\");
}
```
