using Dalamud.Game;
using Dalamud.Logging;
using HUD_Manager;
using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ClientFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

/// <summary>
/// Manages keeping the state of the pet hotbar when HUD changes are made.
/// 
/// It's a little complicated. Basically, in the Character Configuration window, there is a setting to change how the pet
/// hotbar is displayed, either reflecting the actions on hotbar 1 when it appears, or revealing its own hotbar (the "Pet Hotbar"
/// HUD element). Pet hotbar isn't really used for pets anymore, but rather any mount/state change that gives the player new
/// actions. This includes beast tribe mount quests like the Vanu Sanuwa quest, role-playing scenes where you play as an NPC,
/// and vehicles like A4.
/// 
/// The game suffers from a slight bug in which changing the HUD will cause your hotbar 1 to discard the "overlain" pet hotbar
/// and revert to its original set of actions. This is problematic since entering a state in which the pet hotbar is shown
/// can cause a HUD swap depending on the user's configuration, which will lead to the end result of the pet hotbar
/// being thrown out immediately whenever it would appear on screen.
/// 
/// This manager tracks the state of the pet hotbar in memory, and automatically calls the function that populates the pet hotbars
/// whenever a HUD change is made.
/// </summary>
public class PetHotbar : IDisposable
{
    private const int PetHotbarValue = 0xE; // Quest mount with actions, e.g. Namazu Mikoshi quests

    // Known values at HotbarPetTypeOffset:
    //   0x0E  Quest mount, e.g. Namazu Mikoshi quests
    //   0x12  Regular mount with actions, e.g. Logistics Node
    //   0x22  Unknown (found function but not found in game)
    private const int HotbarPetTypeOffset = 0x11970;
    private const int Offset2 = 0x27094;
    private const bool DoFix = false;

    private delegate void ResetPetHotbarDelegate();

    private unsafe delegate void DecidePetHotbarDelegate(uint* ptr0, char a2, int a3);

    private delegate long GetNextModeDelegate(long a1);

    private delegate void SetupPetHotbarFakeDelegate(IntPtr uiModule, byte playSound);

    private delegate void SetupPetHotbarRealDelegate(IntPtr uiModule, uint value);

    private delegate long GetPetHotbarManagerThing(IntPtr thingPointer);

    private delegate void BarActionReplacerDelegate(IntPtr ptr0, IntPtr ptr1, IntPtr ptr2, IntPtr ptr3, int unk0,
        int unk1);

    private readonly IntPtr hotbarModuleAddress;
    private readonly IntPtr uiModuleAddress;

    private readonly Plugin plugin;

    // private readonly ResetPetHotbarDelegate prepareReset;
    // private readonly SetupPetHotbarDelegate performReset;
    private readonly Hook<ResetPetHotbarDelegate> resetPetHotbarHook;
    private readonly Hook<DecidePetHotbarDelegate> decidePetHotbarHook;
    private readonly Hook<GetNextModeDelegate> getNextModeHook;
    private readonly Hook<SetupPetHotbarFakeDelegate> setupPetHotbarFakeHook;
    private readonly Hook<SetupPetHotbarRealDelegate> setupPetHotbarRealHook;
    private readonly GetPetHotbarManagerThing getManagerThing;

    private bool resetInProgress = false;
    private bool prepared = false;

    private bool fixingPvpMount = false;
    private bool fixingPvpMountDoneFirstStage = false;
    private uint fixingPvpMountType;

    private readonly Hook<BarActionReplacerDelegate> barActionReplacerHook;

    public PetHotbar(Plugin plugin)
    {
        this.plugin = plugin;

        unsafe
        {
            var uiModule = ClientFramework.Instance()->GetUiModule();
            this.uiModuleAddress = (IntPtr)uiModule;
            this.hotbarModuleAddress = (IntPtr)uiModule->GetRaptureHotbarModule();
        }

        var resetPetHotbarPtr =
            plugin.SigScanner.ScanText(
                "48 83 EC 28 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 1F 48 8B 10 48 8B C8");
        var decidePetHotbarPtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 85 ED 75 43");
        var getNextModePtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 63 E8 83 FD 0A");
        var setupPetHotbarFakePtr = plugin.SigScanner.ScanText("40 53 48 83 EC 20 83 B9 70 19 01 00 00 48 8B D9 75 14");
        var setupPetHotbarRealPtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 40 83 FD 01");
        var barActionReplacerPtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 49 81 C6 ?? ?? ?? ?? 83 C7 10");


        if (resetPetHotbarPtr == IntPtr.Zero || setupPetHotbarFakePtr == IntPtr.Zero ||
            setupPetHotbarRealPtr == IntPtr.Zero
            || decidePetHotbarPtr == IntPtr.Zero || barActionReplacerPtr == IntPtr.Zero ||
            getNextModePtr == IntPtr.Zero)
        {
            PluginLog.Error(
                "PetHotbar: unable to find one or more signatures. Pet hotbar functionality will be disabled.\n" +
                $"prepare: {resetPetHotbarPtr}\nperform: {setupPetHotbarFakePtr}");
            return;
        }
        else
        {
            PluginLog.Warning($"resetPetHotbarPtr = 0x{resetPetHotbarPtr:X}");
            PluginLog.Warning($"decidePetHotbarPtr = 0x{decidePetHotbarPtr:X}");
            PluginLog.Warning($"getNextModePtr = 0x{getNextModePtr:X}");
            PluginLog.Warning($"setupPetHotbarFakePtr = 0x{setupPetHotbarFakePtr:X}");
            PluginLog.Warning($"setupPetHotbarRealPtr = 0x{setupPetHotbarRealPtr:X}");
            PluginLog.Warning($"barActionReplacerPtr = 0x{barActionReplacerPtr:X}");
        }

        // this.prepareReset = Marshal.GetDelegateForFunctionPointer<ResetPetHotbarDelegate>(preparePetHotbarChangePtr);
        // this.performReset = Marshal.GetDelegateForFunctionPointer<SetupPetHotbarDelegate>(performPetHotbarChangePtr);

        this.resetPetHotbarHook =
            Hook<ResetPetHotbarDelegate>.FromAddress(resetPetHotbarPtr, this.ResetPetHotbarDetour);
        resetPetHotbarHook.Enable();
        unsafe
        {
            this.decidePetHotbarHook =
                Hook<DecidePetHotbarDelegate>.FromAddress(decidePetHotbarPtr,
                    this.DecidePetHotbarDetour);
            decidePetHotbarHook.Enable();
        }

        this.getNextModeHook = Hook<GetNextModeDelegate>.FromAddress(getNextModePtr, this.GetNextModeDetour);
        getNextModeHook.Enable();

        this.setupPetHotbarFakeHook =
            Hook<SetupPetHotbarFakeDelegate>.FromAddress(setupPetHotbarFakePtr, this.SetupPetHotbarFakeDetour);
        setupPetHotbarFakeHook.Enable();
        this.setupPetHotbarRealHook =
            Hook<SetupPetHotbarRealDelegate>.FromAddress(setupPetHotbarRealPtr, this.SetupPetHotbarRealDetour);
        setupPetHotbarRealHook.Enable();

        this.barActionReplacerHook =
            Hook<BarActionReplacerDelegate>.FromAddress(barActionReplacerPtr, this.BarActionReplacerDetour);
        // this.barActionReplacerHook.Enable();

        unsafe
        {
            // 0x14079c16b: CALL qword ptr [RDX + <offset>]
            // Updated 6.08
            const int vtblOffset = 0x68;

            // We want to run the "perform change" function from the game, but the function we hooked here is normally wrapped
            //  by an outer function to perform setup first. We want to disable sounds so we` still must run this internal function,
            //  so this code here is just replicating what the outer function would do.

            // Double dereference to acquire the function address from the table.
            var fnPointer = *(long*)uiModuleAddress + vtblOffset;
            this.getManagerThing =
                Marshal.GetDelegateForFunctionPointer<GetPetHotbarManagerThing>((IntPtr)(*(long*)fnPointer));
        }

        this.plugin.Framework.Update += CheckResetLoop;
        this.plugin.Framework.Update += CheckResetLoop2;
    }

    public void ResetPetHotbarDetour()
    {
        var pvp = true;
        // var pvp = plugin.ClientState.IsPvP;
        if (pvp) {
            var hotbarPetType = unchecked((uint)Marshal.ReadInt32(hotbarModuleAddress + HotbarPetTypeOffset));
            if (hotbarPetType > 0) {
                int petHotbarSwapOption;
                unsafe {
                    petHotbarSwapOption = ConfigModule.Instance()->GetIntValue((short)ConfigOption.ExHotbarChangeHotbar1);
                }
                if (petHotbarSwapOption > 0) {
                    fixStage = FixingPvpPetBar.Setup;
                    fixingPvpMountType = hotbarPetType;
                }
            }

            var petHotbarValue = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);
            var petHotbarValue80 = Marshal.ReadByte(hotbarModuleAddress + Offset2);
            PluginLog.Information(
                $"    v ResetPetHotbarDetour (before): {petHotbarValue == PetHotbarValue}[0x{petHotbarValue:X}/0x{petHotbarValue80:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");
            if (petHotbarValue > 0)
            {
                PluginLog.Warning($"  ResetPetHotbarDetour() // changeHotbarOption={hotbarPetType}");
                this.resetPetHotbarHook.Original();

                petHotbarValue = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);
                petHotbarValue80 = Marshal.ReadByte(hotbarModuleAddress + Offset2);
                PluginLog.Information(
                    $"    ^ ResetPetHotbarDetour (after ): {petHotbarValue == PetHotbarValue}[0x{petHotbarValue:X}/0x{petHotbarValue80:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");
            }
            else
            {
                PluginLog.Warning($"    ^ Follow up skipped (pet bar not active)");
            }
        }
    }

    private unsafe void DecidePetHotbarDetour(uint* ptr0, char a2, int a3)
    {
        var a1 = *ptr0;
        PluginLog.Warning($"DecidePetHotbarDetour(0x{(uint)ptr0:X}, {(uint)a2}, {a3}) / {a1}");
        this.decidePetHotbarHook.Original(ptr0, a2, a3);
    }

    private long GetNextModeDetour(long a1)
    {
        long input;
        unsafe
        {
            input = *(uint*)a1;
        }

        var result = getNextModeHook.Original.Invoke(a1);
        PluginLog.Information($"  GetNextModeDetour [{input}]->[{result}] (0x{a1:X})");
        return result;
    }

    public void SetupPetHotbarFakeDetour(IntPtr uiModule, byte playSound)
    {
        PluginLog.Warning($"  ???SetupPetHotbarDetour(0x{uiModule:X}, {(uint)playSound})");
        this.setupPetHotbarFakeHook.Original(uiModule, playSound);
    }

    public void SetupPetHotbarRealDetour(IntPtr uiModule, uint value)
    {
        var petHotbarValue = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);
        var petHotbarValue80 = Marshal.ReadByte(hotbarModuleAddress + Offset2);
        PluginLog.Information(
            $"    v SetupPetHotbarRealDetour (before): {petHotbarValue == PetHotbarValue}[0x{petHotbarValue:X}/0x{petHotbarValue80:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");

        PluginLog.Warning($"  SetupPetHotbarRealDetour(0x{uiModule:X}, {value})");

        petHotbarValue = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);
        petHotbarValue80 = Marshal.ReadByte(hotbarModuleAddress + Offset2);
        PluginLog.Information(
            $"    ^ SetupPetHotbarRealDetour (after ): {petHotbarValue == PetHotbarValue}[0x{petHotbarValue:X}/0x{petHotbarValue80:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");

        this.setupPetHotbarRealHook.Original(uiModule, value);
    }

    public void BarActionReplacerDetour(IntPtr ptr0, IntPtr ptr1, IntPtr ptr2, IntPtr ptr3, int unk0, int unk1)
    {
        PluginLog.Warning($"BarActionReplacerDetour(0x{ptr0:X}, 0x{ptr1:X}, 0x{ptr2:X}, 0x{ptr3:X}, {unk0}, {unk1})");
        this.barActionReplacerHook.Original(ptr0, ptr1, ptr2, ptr3, unk0, unk1);
    }

    public IntPtr GetManagerThing()
    {
        return (IntPtr)getManagerThing.Invoke(uiModuleAddress);
    }

    public void ResetPetHotbar()
    {
        if (resetInProgress)
            return;

        resetInProgress = true;
        prepared = false;
    }

    private void CheckResetLoop2(Framework _)
    {
        if (fixStage > FixingPvpPetBar.Off)
        {
            if (fixStage == FixingPvpPetBar.Setup)
            {
                PluginLog.Error("P1");
                this.SetupPetHotbarRealDetour(GetManagerThing(), fixingPvpMountType);
                fixStage = FixingPvpPetBar.Reset;
                return;
            }

            PluginLog.Error("P2");
            this.ResetPetHotbarDetour();
            fixStage = FixingPvpPetBar.Off;
        }
    }

    private FixingPvpPetBar fixStage = FixingPvpPetBar.Off;

    enum FixingPvpPetBar
    {
        Off = 0,
        Setup = 1,
        Reset = 2
    }

    private void CheckResetLoop(Framework _)
    {
        // const int petHotbarValue = 0x12;

        var value = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);

        // PluginLog.Information($"<PET> resetInProgress={resetInProgress} prepared={prepared}");
        if (resetInProgress)
        {
            if (!prepared && PetHotbarActive())
            {
                PluginLog.Information(
                    $"  prepareReset (before): {value == PetHotbarValue}[0x{value:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");
                PluginLog.Information($"  prepareReset[{(DoFix ? "REAL" : "SKIPPED")}]");
                if (DoFix)
                {
                    // this.prepareReset.Invoke();
                    this.ResetPetHotbarDetour();
                }

                prepared = true;
                value = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);
                PluginLog.Information(
                    $"  prepareReset (after ): {value == PetHotbarValue}[0x{value:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");
                return;
            }

            if (prepared)
            {
                PluginLog.Information(
                    $"    PerformReset (before): {value == PetHotbarValue}[0x{value:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");
                PluginLog.Information($"    PerformReset[{(DoFix ? "REAL" : "SKIPPED")}]");
                if (DoFix)
                {
                    this.PerformReset(true);
                }

                resetInProgress = false;
                prepared = false;
                value = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);
                PluginLog.Information(
                    $"    PerformReset (after ): {value == PetHotbarValue}[0x{value:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");
            }
            else
            {
                resetInProgress = false;
            }
        }
    }

    private void PerformReset(bool playSound)
    {
        var managerThing = this.getManagerThing.Invoke(uiModuleAddress);
        this.SetupPetHotbarFakeDetour((IntPtr)managerThing, (byte)(playSound ? 1 : 0));
        // this.performReset.Invoke((IntPtr)managerThing, (byte)(playSound ? 1 : 0));
    }

    public bool PetHotbarActive()
    {
        // 0x140633216: CMP dword ptr [RCX + <offset>],0x0
        // Updated 6.08
        // const int petHotbarValue = 0xE;

        var value = Marshal.ReadByte(hotbarModuleAddress + HotbarPetTypeOffset);

        PluginLog.Information(
            $"PetHotbarActive: {value == PetHotbarValue}[0x{value:X}] (0x{hotbarModuleAddress:X} + 0x11970 = 0x{hotbarModuleAddress + HotbarPetTypeOffset:X})");

        return value == PetHotbarValue;
    }

    public void Dispose()
    {
        this.barActionReplacerHook.Disable();
        this.barActionReplacerHook.Dispose();
        this.resetPetHotbarHook.Disable();
        this.resetPetHotbarHook.Dispose();
        this.decidePetHotbarHook.Disable();
        this.decidePetHotbarHook.Dispose();
        this.getNextModeHook.Disable();
        this.getNextModeHook.Dispose();
        this.setupPetHotbarFakeHook.Disable();
        this.setupPetHotbarFakeHook.Dispose();
        this.setupPetHotbarRealHook.Disable();
        this.setupPetHotbarRealHook.Dispose();
        this.plugin.Framework.Update -= CheckResetLoop;
        this.plugin.Framework.Update -= CheckResetLoop2;
    }
}