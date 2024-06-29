namespace HUDManager.Structs.Options;

public class TargetBarOptions
{
    private readonly byte[] _options;

    public bool ShowIndependently
    {
        get => _options[0] == 1;
        set => _options[0] = value ? (byte)1 : (byte)0;
    }

    public TargetBarOptions(byte[] options)
    {
        _options = options;
    }
}
