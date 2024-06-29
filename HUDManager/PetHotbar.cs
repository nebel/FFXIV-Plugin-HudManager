using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Runtime.InteropServices;

namespace HUDManager;

/// <summary>
/// Fixes the pet hotbar on occasions when it becomes "stuck" in PvP when hotbar 1 pet overlays are enabled.
///
/// This class actually works around a bug in the base game. When "Automatically replace hotbar 1 with pet hotbar when
/// mounted" is enabled, swapping HUD slots when mounted in PvP using a mount with mount actions causes the overlaid
/// per bar not to be fixed upon dismounting. This can be replicated without addons by mounting, swapping hud slots
/// manually, then dismounting. However, naturally HUD Manager makes this more likely by causing more swaps to happen.
///
/// To fix this, we detect a potential dismount in resetPetHotbarHook (which called when the game tries to restore the
/// hotbar upon dismounting). However, when this bug happens, the bar will not be restored as expected. To fix this, we
/// force-reenable the pet overlay on the following frame by calling setupPetHotbar (clearing the bugged state) then
/// follow up with yet another call to resetPetHotbarHook on the frame after that.
///
/// I don't know if it's possible to break this fix if an automated HUD swap would happen between these frames. If so,
/// we could block swaps while FixingPvpPetBar is set. However, it seems vanishingly unlikely to happen, and could
/// simply be fixed by remounting it if did.
/// </summary>
public sealed class PetHotbar : IDisposable
{
    // Known values at HotbarPetTypeOffset:
    //   0x0E  Quest mount, e.g. Namazu Mikoshi quests
    //   0x12  Regular mount with actions, e.g. Logistics Node
    //   0x22  Unknown (found function but not found in game)
    // (Last updated in 6.4)
    private const int HotbarPetTypeOffset = 0x11970;

    private delegate void ResetPetHotbarDelegate();

    private delegate void SetupPetHotbarDelegate(IntPtr uiModule, uint value);

    private readonly Plugin _plugin;

    private readonly IntPtr _raptureHotbarModulePtr;
    private readonly IntPtr _hotbarPetTypePtr;

    // We can safely set these to null because we exit the constructor before these can ever be used if either sig scan fails
    private readonly SetupPetHotbarDelegate _setupPetHotbar = null!;
    private readonly Hook<ResetPetHotbarDelegate> _resetPetHotbarHook = null!;

    private FixingPvpPetBar _fixStage = FixingPvpPetBar.Off;
    private uint _fixPetType;

    public PetHotbar(Plugin plugin)
    {
        _plugin = plugin;

        unsafe {
            _raptureHotbarModulePtr = (nint)UIModule.Instance()->GetRaptureHotbarModule();
            _hotbarPetTypePtr = _raptureHotbarModulePtr + HotbarPetTypeOffset;
        }

        var resetPetHotbarPtr = plugin.SigScanner.ScanText("48 83 EC 28 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 1F 48 8B 10 48 8B C8");
        var setupPetHotbarRealPtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 40 83 FD 01");

        if (setupPetHotbarRealPtr == IntPtr.Zero || resetPetHotbarPtr == IntPtr.Zero) {
            plugin.Log.Error(
                "PetHotbar: unable to find one or more signatures. Pet hotbar functionality will be disabled.\n" +
                $"setup: {setupPetHotbarRealPtr}\nreset: 0x{resetPetHotbarPtr:X}");
            return;
        }

        _setupPetHotbar = Marshal.GetDelegateForFunctionPointer<SetupPetHotbarDelegate>(setupPetHotbarRealPtr);
        _resetPetHotbarHook = plugin.GameInteropProvider.HookFromAddress<ResetPetHotbarDelegate>(resetPetHotbarPtr, ResetPetHotbarDetour);
        _resetPetHotbarHook.Enable();

        _plugin.Framework.Update += CheckFixLoop;
    }

    private void ResetPetHotbarDetour()
    {
        if (_plugin.ClientState.IsPvP && _fixStage == FixingPvpPetBar.Off) {
            var hotbarPetType = unchecked((uint)Marshal.ReadInt32(_hotbarPetTypePtr));
            if (hotbarPetType > 0) {
                if (_plugin.GameConfig.UiConfig.TryGet("ExHotbarChangeHotbar1", out bool isPetOverlayEnabled) && isPetOverlayEnabled) {
                    _plugin.Log.Debug($"PetHotbarFix F0: Detected potentially broken pet hotbar overlay (0x{hotbarPetType:X}). Fixing...");
                    _fixStage = FixingPvpPetBar.Setup;
                    _fixPetType = hotbarPetType;
                }
            }
        }

        _resetPetHotbarHook.Original();
    }

    private void CheckFixLoop(IFramework _)
    {
        if (_fixStage > FixingPvpPetBar.Off) {
            if (_fixStage == FixingPvpPetBar.Setup) {
                _plugin.Log.Debug("PetHotbarFix F1: Setting hotbar back to mounted state");
                _setupPetHotbar(_raptureHotbarModulePtr, _fixPetType);
                _fixStage = FixingPvpPetBar.Reset;
                return;
            }

            _plugin.Log.Debug("PetHotbarFix F2: Resetting hotbar");
            _resetPetHotbarHook.Original();
            _fixStage++;
            _fixStage = FixingPvpPetBar.Off;
        }
    }

    private enum FixingPvpPetBar
    {
        Off = 0,
        Setup = 1,
        Reset = 2,
    }

    public void Dispose()
    {
        _resetPetHotbarHook.Disable();
        _resetPetHotbarHook.Dispose();
        _plugin.Framework.Update -= CheckFixLoop;
    }
}
