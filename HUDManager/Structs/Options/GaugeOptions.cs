namespace HUDManager.Structs.Options;

public class GaugeOptions
{
    private readonly byte[] _options;

    public GaugeStyle Style
    {
        get => (GaugeStyle)_options[0];
        set => _options[0] = (byte)value;
    }

    public GaugeOptions(byte[] options)
    {
        _options = options;
    }
}

public enum GaugeStyle : byte
{
    Normal = 0,
    Simple = 1,
}
