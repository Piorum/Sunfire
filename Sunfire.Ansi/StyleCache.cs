using Sunfire.Ansi.Models;
using Sunfire.Shared;

namespace Sunfire.Ansi;

public class StyleCache : IdIndexedCache<(SColor? fgColor, SColor? bgColor, SAnsiProperty properties), StyleData, int>
{
    protected override int CreateInfo(int id, StyleData dataOjbect) =>
        id;

    protected override StyleData CreateObject((SColor? fgColor, SColor? bgColor, SAnsiProperty properties) creationData) =>
        new() { ForegroundColor = creationData.fgColor, BackgroundColor = creationData.bgColor, Properties = creationData.properties };

    protected override StyleData Update(StyleData dataObject, (SColor? fgColor, SColor? bgColor, SAnsiProperty properties) creationData) =>
        dataObject with { ForegroundColor = creationData.fgColor, BackgroundColor = creationData.bgColor, Properties = creationData.properties };
}
