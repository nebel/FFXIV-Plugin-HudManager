namespace HUDManager.Structs.Options;

public class PartyListOptions
{
    private readonly byte[] _options;

    public PartyListAlignment Alignment
    {
        get => (PartyListAlignment)_options[0];
        set => _options[0] = (byte)value;
    }

    public PartyListOptions(byte[] options)
    {
        _options = options;
    }
}

public enum PartyListAlignment : byte
{
    Top = 0,
    Bottom = 1,
}
