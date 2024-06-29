using System;
using System.Runtime.InteropServices;

namespace HUDManager;

public class GameFunctions
{
    // private delegate IntPtr GetBaseUiObjectDelegate();

    private delegate void SetPositionDelegate(IntPtr windowBase, short x, short y);

    private delegate void SetAlphaDelegate(IntPtr windowBase, byte alpha);

    private delegate byte UpdateAddonPositionDelegate(IntPtr raptureAtkUnitManager, IntPtr addon, byte clicked);

    private readonly SetPositionDelegate _setPosition;
    private readonly UpdateAddonPositionDelegate _updateAddonPosition;

    private Plugin Plugin { get; }

    public GameFunctions(Plugin plugin)
    {
        Plugin = plugin;

        var setPositionPtr = Plugin.SigScanner.ScanText("41 0F BF C0 66 89 91 ?? ?? ?? ??"); // Component::GUI::AtkUnitBase_SetPosition
        // var setAlphaPtr = Plugin.SigScanner.ScanText("F6 81 ?? ?? ?? ?? ?? 88 91 ?? ?? ?? ??"); // Component::GUI::AtkUnitBase_SetAlpha
        var updatePositionPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 33 D2 48 8B 01 FF 90 ?? ?? ?? ??");
        // var baseUiPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 0F BF D5");

        _setPosition = Marshal.GetDelegateForFunctionPointer<SetPositionDelegate>(setPositionPtr);
        _updateAddonPosition = Marshal.GetDelegateForFunctionPointer<UpdateAddonPositionDelegate>(updatePositionPtr);
    }

    public void SetAddonPosition(string uiName, short x, short y)
    {
        var addon = Plugin.GameGui.GetAddonByName(uiName);
        if (addon == IntPtr.Zero) {
            return;
        }

        var baseUi = Plugin.GameGui.GetUIModule();
        var manager = Marshal.ReadIntPtr(baseUi + 0x20);

        _updateAddonPosition(
            manager,
            addon,
            1
        );
        _setPosition(addon, x, y);
        _updateAddonPosition(
            manager,
            addon,
            0
        );
    }

    public Vector2<short>? GetAddonPosition(string uiName)
    {
        var addon = Plugin.GameGui.GetAtkUnitByName(uiName, 1);
        if (addon == null) {
            return null;
        }

        return new Vector2<short>(addon.Value.X, addon.Value.Y);
    }
}
