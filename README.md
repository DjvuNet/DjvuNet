DjvuNet
=======

DjvuNet is a fully managed Djvu reader in C# and supports the entire Djvu file structure. This was created to support my commercial activities as Djvu is a superior format for certain document types. DjvuNet is a faithful implementation of the Djvu specs and should work fine for most situations. It is fully usable, but probably needs to be optimized a bit as Djvu is complicated.

Optimal performance will be achieved with some background thread processing.

**Usage**
`````c#
DjvuDocument doc = new DjvuDocument(@"Mcguffey's_Primer.djvu");

var page = doc.Pages[0];

page
    .BuildPageImage()
    .Save("TestImage1.png", ImageFormat.Png);

page.IsInverted = true;

page
    .BuildPageImage()
    .Save("TestImage2.png", ImageFormat.Png);
`````
