using System.Runtime.CompilerServices;
using Sunfire.Enums;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;

namespace Sunfire.Registries;

public class MediaRegistry
{
    public static readonly FileTypeScanner<FileType> Scanner = new();

    private static readonly Dictionary<FileType, MediaType> MediaTypeMap = new()
    {
        {FileType.mp4, MediaType.Video},

        {FileType.jpg, MediaType.Image},
        {FileType.jpeg, MediaType.Image},
        {FileType.webp, MediaType.Image},

        {FileType.zip, MediaType.Archive},
        {FileType.jar, MediaType.Archive},
        {FileType.tar, MediaType.Archive},
    };

    private static readonly Opener fallbackOpener = new() { handler = "xdg-open" };
    private static readonly Dictionary<FileType, Opener> MiscOpenerMap = new()
    {   
        //Example
        //{FileType.dmg, new() { handler = "dmg2img", args = (entry) => $"-i {entry.Path} -o output.img"}},
    };
    private static readonly Dictionary<MediaType, Opener> OpenerMap = new()
    {
        {MediaType.Video, new() { handler = "mpv" }},
    };

    [ModuleInitializer]
    public static void Init()
    {
        //Video
        Scanner.AddFastSignature([0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D], 4, (FileType.mp4, null));
        Scanner.AddFastSignature([0x66, 0x74, 0x79, 0x70, 0x4D, 0x53, 0x4E, 0x56], 4, (FileType.mp4, null));

        //Image
        Scanner.AddFastSignature([0x52, 0x49, 0x46, 0x46, ..Enumerable.Repeat<byte?>(null, 4), 0x57, 0x45, 0x42, 0x50], 0, (FileType.webp, null));
        Scanner.AddFastSignature([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01], 0, (FileType.jpg, new() { {".jpg", FileType.jpg}, {".jpeg", FileType.jpeg} }));

        //Archive
        Scanner.AddFastSignature([0x50, 0x4B, 0x03, 0x04], 0, (FileType.zip, new() { {".zip", FileType.zip}, {".jar", FileType.jar} }));
        Scanner.AddFastSignature([0x75, 0x73, 0x74, 0x61, 0x72, 0x00, 0x30, 0x30], 257, (FileType.tar, null));
        Scanner.AddFastSignature([0x75, 0x73, 0x74, 0x61, 0x72, 0x20, 0x20, 0x00], 257, (FileType.tar, null));

        //Misc
        //Example: Scanner.AddSlowSignature([0x6B, 0x6F, 0x6C, 0x79], [508], ".dmg", FileType.dmg, fromEnd: true);
    }

    public static MediaType GetMediaType(FSEntry entry) =>
        GetMediaType(Scanner.Scan(entry));
    public static MediaType GetMediaType(FileType fileType) =>
        MediaTypeMap.GetValueOrDefault(fileType);

    public static Opener GetOpener(FSEntry entry)
    {
        FileType fileType = Scanner.Scan(entry);

        //Prio file type openers over generic media type opener
        if(!MiscOpenerMap.TryGetValue(fileType, out var opener))
            //Try to get generic media type opener, if this fails use fallback
            if (!MediaTypeMap.TryGetValue(fileType, out var mediaType) || !OpenerMap.TryGetValue(mediaType, out opener))
                opener = fallbackOpener;

        return opener;

    }

    public struct Opener()
    {
        required public string handler;

        public Func<FSEntry, string> args = (entry) => $"{entry.Path}";
    }
}
