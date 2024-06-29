namespace HUDManager.Structs.Options;

public class StatusBaseOptions
{
    private readonly byte[] _options;

    public StatusBaseAlignment Alignment
    {
        get => (StatusBaseAlignment)(_options[0] & 0xF0);
        set => _options[0] = (byte)((byte)Grouping | (byte)value);
    }

    public StatusBaseGrouping Grouping
    {
        get => (StatusBaseGrouping)(_options[0] & 0xF);
        set => _options[0] = (byte)((byte)Alignment | (byte)value);
    }

    public StatusBaseOptions(byte[] options)
    {
        _options = options;
    }
}

public enum StatusBaseGrouping : byte
{
    Normal = 0x1,
    ThreeGroups = 0x0,
    FourGroups = 0x2,
}

public enum StatusBaseAlignment : byte
{
    LeftJustified1 = 0x10,
    LeftJustified2 = 0x20,
    LeftJustified3 = 0x30,
}

public static class StatusBaseExt
{
    public static string Name(this StatusBaseAlignment alignment)
    {
        return alignment switch
        {
            StatusBaseAlignment.LeftJustified1 => "Left-justified I",
            StatusBaseAlignment.LeftJustified2 => "Left-justified II",
            StatusBaseAlignment.LeftJustified3 => "Left-justified III",
            _ => alignment.ToString(),
        };
    }

    public static string Name(this StatusBaseGrouping grouping)
    {
        return grouping switch
        {
            StatusBaseGrouping.Normal => "Display as Single Element",
            StatusBaseGrouping.ThreeGroups => "Split Element into 3 Groups",
            StatusBaseGrouping.FourGroups => "Split Element into 4 Groups",
            _ => grouping.ToString(),
        };
    }

    public static readonly StatusBaseGrouping[] StatusGroupingOrder =
    [
        StatusBaseGrouping.Normal,
        StatusBaseGrouping.ThreeGroups,
        StatusBaseGrouping.FourGroups,
    ];
}
