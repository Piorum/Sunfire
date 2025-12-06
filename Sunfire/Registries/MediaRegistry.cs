using System.Runtime.CompilerServices;
using Sunfire.Enums;
using Sunfire.FSUtils;

namespace Sunfire.Registries;

public class MediaRegistry
{
    public static readonly MediaTypeScanner<MediaType> Scanner = new();


    [ModuleInitializer]
    public static void Init()
    {
        //Video
        Scanner.AddFastSignature([0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D], 4, (MediaType.mp4, null));
        Scanner.AddFastSignature([0x66, 0x74, 0x79, 0x70, 0x4D, 0x53, 0x4E, 0x56], 4, (MediaType.mp4, null));

        //Image
        Scanner.AddFastSignature([0x52, 0x49, 0x46, 0x46, ..Enumerable.Repeat<byte?>(null, 4), 0x57, 0x45, 0x42, 0x50], 0, (MediaType.webp, null));
        Scanner.AddFastSignature([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01], 0, (MediaType.jpg, new() { {".jpg", MediaType.jpg}, {".jpeg", MediaType.jpeg} }));

        //Archive
        Scanner.AddFastSignature([0x50, 0x4B, 0x03, 0x04], 0, (MediaType.zip, new() { {".zip", MediaType.zip}, {".jar", MediaType.jar} }));
        Scanner.AddFastSignature([0x75, 0x73, 0x74, 0x61, 0x72, 0x00, 0x30, 0x30], 257, (MediaType.tar, null));
        Scanner.AddFastSignature([0x75, 0x73, 0x74, 0x61, 0x72, 0x20, 0x20, 0x00], 257, (MediaType.tar, null));

        //Misc
        Scanner.AddSlowSignature([0x6B, 0x6F, 0x6C, 0x79], [508], ".dmg", MediaType.dmg, fromEnd: true);
    }
}
