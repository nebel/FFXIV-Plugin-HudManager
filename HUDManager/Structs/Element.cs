using System;

namespace HUDManager.Structs;

[Serializable]
public class Element
{
    public const ElementComponent AllEnabled = ElementComponent.X
        | ElementComponent.Y
        | ElementComponent.Scale
        | ElementComponent.Visibility
        | ElementComponent.Opacity
        | ElementComponent.Options;

    public ElementKind Id { get; set; }

    public ElementComponent Enabled { get; set; } = AllEnabled;

    public ElementLayoutFlags LayoutFlags { get; set; } = ElementLayoutFlags.None;

    public float X { get; set; }

    public float Y { get; set; }

    public float Scale { get; set; }

    public byte[]? Options { get; set; }

    public ushort Width { get; set; }

    public ushort Height { get; set; }

    public MeasuredFrom MeasuredFrom { get; set; }

    public VisibilityFlags Visibility { get; set; }

    public byte Unknown6 { get; set; }

    public byte Opacity { get; set; }

    public byte[]? Unknown8 { get; set; }

    public bool this[VisibilityFlags flags]
    {
        get => (Visibility & flags) > 0;
        set
        {
            if (value) {
                Visibility |= flags;
            } else {
                Visibility &= ~flags;
            }
        }
    }

    public bool this[ElementComponent component]
    {
        get => (Enabled & component) > 0;
        set
        {
            if (value) {
                Enabled |= component;
            } else {
                Enabled &= ~component;
            }
        }
    }

#pragma warning disable 8618
    private Element()
    {
    }
#pragma warning restore 8618

    public Element(RawElement raw)
    {
        Id = raw.id;
        X = raw.x;
        Y = raw.y;
        Scale = raw.scale;
        Options = raw.options;
        Width = raw.width;
        Height = raw.height;
        MeasuredFrom = raw.measuredFrom;
        Visibility = raw.visibility;
        Unknown6 = raw.unknown6;
        Opacity = raw.opacity;
        Unknown8 = raw.unknown8;
    }

    public Element Clone()
    {
        return new()
        {
            Enabled = Enabled,
            Id = Id,
            LayoutFlags = LayoutFlags,
            X = X,
            Y = Y,
            Scale = Scale,
            Options = (byte[]?)Options?.Clone(),
            Width = Width,
            Height = Height,
            MeasuredFrom = MeasuredFrom,
            Visibility = Visibility,
            Unknown6 = Unknown6,
            Opacity = Opacity,
            Unknown8 = (byte[]?)Unknown8?.Clone(),
        };
    }

    public void UpdateEnabled(Element other)
    {
        if (other[ElementComponent.X]) {
            X = other.X;
        }

        if (other[ElementComponent.Y]) {
            Y = other.Y;
        }

        if (other[ElementComponent.Scale]) {
            Scale = other.Scale;
        }

        if (other[ElementComponent.Visibility]) {
            Visibility = other.Visibility;
        }

        if (other[ElementComponent.Opacity]) {
            Opacity = other.Opacity;
        }

        if (other[ElementComponent.Options]) {
            Options = other.Options;
        }

        Height = other.Height;
        Width = other.Width;
        MeasuredFrom = other.MeasuredFrom;
        Unknown6 = other.Unknown6;
        Unknown8 = other.Unknown8;
    }
}

[Flags]
public enum ElementComponent : uint
{
    X = 1 << 0,
    Y = 1 << 1,
    Scale = 1 << 2,
    Visibility = 1 << 3,
    Opacity = 1 << 4,
    Options = 1 << 5,
}

[Flags]
public enum ElementLayoutFlags : uint
{
    None = 0,
    ClobberTransientOptions = 1 << 0,
}
