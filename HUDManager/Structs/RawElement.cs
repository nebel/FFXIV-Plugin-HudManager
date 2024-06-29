using System;
using System.Runtime.InteropServices;

namespace HUDManager.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct RawElement
{
    public ElementKind id;

    public float x;

    public float y;

    public float scale;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[]? options;

    public ushort width;

    public ushort height;

    public MeasuredFrom measuredFrom;

    public VisibilityFlags visibility;

    public byte unknown6;

    public byte opacity;

    // last two bytes are padding
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[]? unknown8;

    public RawElement(Element element)
    {
        id = element.Id;
        x = element.X;
        y = element.Y;
        scale = element.Scale;
        options = element.Options;
        width = element.Width;
        height = element.Height;
        measuredFrom = element.MeasuredFrom;
        visibility = element.Visibility;
        unknown6 = element.Unknown6;
        opacity = element.Opacity;
        unknown8 = element.Unknown8;
    }

    public void UpdateEnabled(Element element)
    {
        if (element[ElementComponent.X]) {
            x = element.X;
        }

        if (element[ElementComponent.Y]) {
            y = element.Y;
        }

        if (element[ElementComponent.Scale]) {
            scale = element.Scale;
        }

        if (element[ElementComponent.Visibility]) {
            visibility = element.Visibility;
        }

        if (element[ElementComponent.Opacity]) {
            opacity = element.Opacity;
        }

        if (element[ElementComponent.Options]) {
            options = element.Options;
        }
    }

    public override string ToString()
    {
        return $"{nameof(id)}: {id}, " +
            $"{nameof(x)}: {x}, " +
            $"{nameof(y)}: {y}, " +
            $"{nameof(scale)}: {scale}, " +
            $"{nameof(options)}: {(options == null ? "<null>" : BitConverter.ToString(options))}, " +
            $"{nameof(width)}: {width}, " +
            $"{nameof(height)}: {height}, " +
            $"{nameof(measuredFrom)}: {measuredFrom}, " +
            $"{nameof(visibility)}: {visibility}, " +
            $"{nameof(unknown6)}: 0x{unknown6:X}, " +
            $"{nameof(opacity)}: 0x{opacity:X}, " +
            $"{nameof(unknown8)}: {(unknown8 == null ? "<null>" : BitConverter.ToString(unknown8[..6]))} ({(unknown8 == null ? "<null>" : BitConverter.ToString(unknown8[6..]))})";
    }
}
