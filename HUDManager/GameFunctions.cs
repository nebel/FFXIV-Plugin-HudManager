using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HUDManager;

public unsafe class GameFunctions
{
    [Signature("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 33 D2 48 8B 01 FF 90 ?? ?? ?? ??")]
    private readonly delegate* unmanaged<ConfigBase.ChangeEventInterface*, AtkUnitBase*, byte, void>
        _updateAddonPosition = null!;

    [Signature("E8 ?? ?? ?? ?? 4C 8B 7C 24 ?? 41 C6 46 ?? ??")]
    private readonly delegate* unmanaged<AddonConfig*, void> _applyHudLayout = null!;

    public GameFunctions()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void SetAddonPosition(string uiName, short x, short y)
    {
        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName(uiName).Address;
        if (addon is null) {
            return;
        }

        var uiModule = (UIModule*)Service.GameGui.GetUIModule().Address;
        var changeEventInterface = uiModule->NextInterface;
        // Service.Log.Debug($"0x{(nint)changeEventInterface:X} / 0x{Marshal.ReadIntPtr((nint)uiModule + 0x20):X}");
        if (changeEventInterface is null) {
            return;
        }

        _updateAddonPosition(changeEventInterface, addon, 1);
        addon->SetPosition(x, y);
        _updateAddonPosition(changeEventInterface, addon, 0);
    }

    public void ApplyHudLayout()
    {
        _applyHudLayout(AddonConfig.Instance());
    }

    public Vector2<short>? GetAddonPosition(string uiName)
    {
        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName(uiName).Address;
        if (addon is null) {
            return null;
        }
        return new Vector2<short>(addon->X, addon->Y);
    }
}