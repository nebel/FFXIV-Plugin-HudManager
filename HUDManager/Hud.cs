﻿using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDManager.Configuration;
using HUDManager.Structs;
using HUDManager.Structs.External;
using HUDManager.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace HUDManager;

public sealed class Hud : IDisposable
{
    public const int InMemoryLayoutElements = 108; // Updated 7.0
    // Each element is 32 bytes in ADDON.DAT, but they're 36 bytes when loaded into memory.
    private const int LayoutSize = InMemoryLayoutElements * 36; // Updated 5.45

    private const int FileDataPointerOffset = 0x50;
    private const int FileSaveMarkerOffset = 0x3E; // Unused

    private const int DataSlotOffset = 0xC8E0; // Updated 7.0
    private const int DataBaseLayoutOffset = 0x6498; // Updated 6.51
    private const int DataDefaultLayoutOffset = 0x35F8; // Updated 6.51 (note: not used except in debug window, not sure of exact structure)

    private delegate IntPtr GetFilePointerDelegate(byte index);
    private delegate uint SetHudLayoutDelegate(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1);
    private readonly GetFilePointerDelegate? _getFilePointer;
    private readonly SetHudLayoutDelegate? _setHudLayout;

    private StagingState? _stagingState;

    private Plugin Plugin { get; }

    private record StagingState(uint JobId, Guid LayoutId, List<Guid> LayerIds)
    {
        public bool SameLayers(Guid layoutId, List<Guid> layerIds) => LayoutId == layoutId && LayerIds.SequenceEqual(layerIds);
        public bool SameJob(uint playerJobId) => JobId == playerJobId;
    }

    public Hud(Plugin plugin)
    {
        Plugin = plugin;

        var getFilePointerPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
        var setHudLayoutPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 EB 12"); // Client::UI::Misc::AddonConfig_ChangeHudLayout
        if (getFilePointerPtr != IntPtr.Zero) {
            _getFilePointer = Marshal.GetDelegateForFunctionPointer<GetFilePointerDelegate>(getFilePointerPtr);
        }

        if (setHudLayoutPtr != IntPtr.Zero) {
            _setHudLayout = Marshal.GetDelegateForFunctionPointer<SetHudLayoutDelegate>(setHudLayoutPtr);
        }
    }

    public IntPtr GetFilePointer(byte index)
    {
        return _getFilePointer?.Invoke(index) ?? IntPtr.Zero;
    }

    public void SaveAddonData()
    {
        var saveMarker = GetFilePointer(0) + FileSaveMarkerOffset;
        Marshal.WriteByte(saveMarker, 1);
    }

    public void SelectSlot(HudSlot slot, bool force = false)
    {
        if (_setHudLayout == null) {
            return;
        }

        var file = GetFilePointer(0);
        // change the current slot so the game lets us pick one that's currently in use
        if (!force) {
            goto Return;
        }

        unsafe {
            var currentSlotPtr = (uint*)(GetDataPointer() + DataSlotOffset);
            // read the current slot
            var currentSlot = *currentSlotPtr;
            // if the current slot is the slot we want to change to, we can force a reload by
            // telling the game it's on a different slot and swapping back to the desired slot
            if (currentSlot == (uint)slot) {
                var backupSlot = currentSlot;
                if (backupSlot < 3) {
                    backupSlot += 1;
                } else {
                    backupSlot = 0;
                }

                // back up this different slot
                var backup = ReadLayout((HudSlot)backupSlot);
                // change the current slot in memory
                *currentSlotPtr = backupSlot;

                // ask the game to change slot to our desired slot
                // for some reason, this overwrites the current slot, so this is why we back up
                _setHudLayout.Invoke(file, (uint)slot, 0, 1);
                // restore the backup
                WriteLayout((HudSlot)backupSlot, backup, false);
                return;
            }
        }

        Return:
        _setHudLayout.Invoke(file, (uint)slot, 0, 1);
    }

    public unsafe static IntPtr GetDataPointer()
    {
        // Plugin.Log.Debug($"1 filePointer(0) 0x{this.GetFilePointer(0):X} + offset 0x{FileDataPointerOffset:X} = 0x{(this.GetFilePointer(0) + FileDataPointerOffset):X}");
        // unsafe {
        //     Plugin.Log.Debug($"2 filePointer(0) 0x{(nint)AddonConfig.Instance():X} + offset = 0x{(nint)AddonConfig.Instance()->ModuleData:X}");
        // }
        // var dataPtr = this.GetFilePointer(0) + FileDataPointerOffset;
        // return Marshal.ReadIntPtr(dataPtr);
        return (nint)AddonConfig.Instance()->ModuleData;
    }

    internal static IntPtr GetDefaultLayoutPointer()
    {
        return GetDataPointer() + DataDefaultLayoutOffset;
    }

    unsafe internal static IntPtr GetLayoutPointer(HudSlot slot)
    {
        var slotNum = (int)slot;
        // Plugin.Log.Debug($"layoutPointer({slot}) 0x{this.GetDataPointer():X} + offset 0x{DataBaseLayoutOffset:X} + 0x{slotNum * LayoutSize:X} = 0x{this.GetDataPointer() + DataBaseLayoutOffset + slotNum * LayoutSize:X}");
        // return this.GetDataPointer() + DataBaseLayoutOffset + slotNum * LayoutSize;
        return (nint)AddonConfig.Instance()->ModuleData + 0x8C20 + slotNum * LayoutSize;
    }

    public static HudSlot GetActiveHudSlot()
    {
        // Plugin.Log.Debug($"dataPointer(0x{this.GetDataPointer():X} + offset 0x{DataSlotOffset:X} = 0x{this.GetDataPointer() + DataSlotOffset:X}");
        // Plugin.Log.Debug($"dataPointer2(0x{this.GetDataPointer():X} + offset 0x{DataSlotOffset:X} = 0x{this.GetDataPointer() + DataSlotOffset:X}");
        var slotVal = Marshal.ReadInt32(GetDataPointer() + DataSlotOffset);

        if (!Enum.IsDefined(typeof(HudSlot), slotVal)) {
            throw new IOException($"invalid hud slot in FFXIV memory of ${slotVal}");
        }

        return (HudSlot)slotVal;
    }

    public static Layout ReadLayout(HudSlot slot)
    {
        var slotPtr = GetLayoutPointer(slot);
        return Marshal.PtrToStructure<Layout>(slotPtr);
    }

    private void WriteLayout(HudSlot slot, Layout layout, bool reloadIfNecessary = true)
    {
        WriteLayout(slot, layout.ToDictionary(), reloadIfNecessary);
    }

    private void WriteLayout(HudSlot slot, IReadOnlyDictionary<ElementKind, Element> dict, bool reloadIfNecessary = true)
    {
        var slotPtr = GetLayoutPointer(slot);

        // update existing elements with saved data instead of wholesale overwriting
        var slotLayout = ReadLayout(slot);
#if !READONLY
            for (var i = 0; i < slotLayout.elements.Length; i++) {
                if (!slotLayout.elements[i].id.IsRealElement())
                    continue;

                if (!dict.TryGetValue(slotLayout.elements[i].id, out var element))
                    continue;

                if (reloadIfNecessary) {
                    if (element.Id is ElementKind.Minimap) {
                        // Minimap: Don't load zoom/rotation from HUD settings but use current UI state instead
                        element = element.Clone();
                        element.Options = slotLayout.elements[i].options;
                    } else if (element.Id is ElementKind.Hotbar1
                               && (element.LayoutFlags & ElementLayoutFlags.ClobberTransientOptions) == 0) { // Clobber flag is unset (default)
                        // Hotbar1: Keep cycling state
                        element = element.Clone();
                        element.Options![0] = slotLayout.elements[i].options![0];
                    }
                }

                // just replace the struct if all options are enabled
                if (element.Enabled == Element.AllEnabled) {
                    slotLayout.elements[i] = new RawElement(element);
                    continue;
                }

                // otherwise only replace the enabled options
                slotLayout.elements[i].UpdateEnabled(element);
            }

            Marshal.StructureToPtr(slotLayout, slotPtr, false);

            // copy directly over
            // Marshal.StructureToPtr(layout, slotPtr, false);

            if (!reloadIfNecessary) {
                return;
            }

            var currentSlot = GetActiveHudSlot();
            if (currentSlot == slot) {
                SelectSlot(currentSlot, true);
            }
#endif
    }

    private SavedLayout? GetEffectiveLayout(Guid id, List<Guid>? layers = null)
    {
        // find the node for this id
        var nodes = Node<SavedLayout>.BuildTree(Plugin.Config.Layouts);
        var node = nodes.Find(id);
        if (node == null) {
            return null;
        }

        var elements = new Dictionary<ElementKind, Element>();
        var windows = new Dictionary<string, Window>();
        var bwOverlays = new List<BrowsingwayOverlay>();
        CrossUpConfig? crossUpConfig;

        // Apply each element of a layout on top of the virtual layout we are constructing.
        void ApplyLayout(Node<SavedLayout> node)
        {
            foreach (var element in node.Value.Elements) {
                if (element.Value.Enabled == Element.AllEnabled || !elements.ContainsKey(element.Key)) {
                    elements[element.Key] = element.Value.Clone();
                    continue;
                }

                elements[element.Key].UpdateEnabled(element.Value);
            }

            foreach (var window in node.Value.Windows) {
                if (window.Value.Enabled == Window.AllEnabled || !windows.ContainsKey(window.Key)) {
                    windows[window.Key] = window.Value.Clone();
                    continue;
                }

                windows[window.Key].UpdateEnabled(window.Value);
            }

            foreach (var overlay in node.Value.BrowsingwayOverlays) {
                if (!bwOverlays.Exists(o => o.CommandName == overlay.CommandName)) {
                    bwOverlays.Add(overlay.Clone());
                    continue;
                }

                var findOverlay = bwOverlays.Find(o => o.CommandName == overlay.CommandName);
                if (findOverlay is null) {
                    Plugin.Log.Error("Unable to find overlay during ancestor search");
                    continue;
                }
                findOverlay.UpdateEnabled(overlay);
            }

            crossUpConfig = node.Value.CrossUpConfig?.Clone();

        }

        // get the ancestors and their elements for this node
        foreach (var ancestor in node.Ancestors().Reverse()) {
            ApplyLayout(ancestor);
        }

        ApplyLayout(node);

        // If there's layers, apply them.
        if (Plugin.Config.AdvancedSwapMode && layers != null) {
            foreach (var layerId in layers.Reverse<Guid>()) {
                var layer = nodes.Find(layerId);
                if (layer == null) {
                    Plugin.Log.Error("unable to find layered condition by ID");
                    break;
                }

                ApplyLayout(layer);
            }
        }

        return new SavedLayout($"Effective {id}", elements, windows, bwOverlays, crossUpConfig, Guid.Empty);
    }

    private string GetDebugName(Guid id, List<Guid>? layers) =>
        $"{Plugin.Config.Layouts[id].Name} [{(layers == null ? "" : string.Join(", ", layers.ConvertAll(layer => Plugin.Config.Layouts[layer].Name)))}]";

    public void WriteEffectiveLayoutIfChanged(HudSlot slot, Guid id, List<Guid> layers)
    {
        if (_stagingState != null && _stagingState.SameLayers(id, layers)) {
            if (_stagingState.SameJob(Util.GetPlayerJobId(Plugin))) {
                Plugin.Log.Debug($"Skipped layout {GetDebugName(id, layers)} (state unchanged)");
            } else {
                Plugin.Log.Debug($"Skipped layout {GetDebugName(id, layers)} (gauge changes only)");
                WriteEffectiveLayoutGaugesOnly(id, layers);
            }
            return;
        }

        WriteEffectiveLayout(slot, id, layers);
    }

    private void WriteEffectiveLayoutGaugesOnly(Guid id, List<Guid>? layers = null)
    {
        var effective = GetEffectiveLayout(id, layers);
        if (effective == null) {
            return;
        }

        ApplyAllJobGaugeVisibility(effective);

        _stagingState = new StagingState(Util.GetPlayerJobId(Plugin), id, layers ?? []);
    }

    public void WriteEffectiveLayout(HudSlot slot, Guid id, List<Guid>? layers = null)
    {
        var effective = GetEffectiveLayout(id, layers);
        if (effective == null) {
            return;
        }

        Plugin.Log.Debug($"Writing layout {GetDebugName(id, layers)}");

        WriteLayout(slot, effective.Elements);

        ApplyAllJobGaugeVisibility(effective);

        foreach (var window in effective.Windows) {
            Plugin.GameFunctions.SetAddonPosition(window.Key, window.Value.Position.X, window.Value.Position.Y);
        }

        foreach (var overlay in effective.BrowsingwayOverlays) {
            overlay.ApplyOverlay(Plugin);
        }

        effective.CrossUpConfig?.ApplyConfig(Plugin);

        _stagingState = new StagingState(Util.GetPlayerJobId(Plugin), id, layers ?? []);
    }

    internal void ImportSlot(string name, HudSlot slot, bool save = true)
    {
        Import(name, ReadLayout(slot), save);
    }

    private void Import(string name, Layout layout, bool save = true)
    {
        var guid = Plugin.Config.Layouts.FirstOrDefault(kv => kv.Value.Name == name).Key;
        guid = guid != default ? guid : Guid.NewGuid();

        Plugin.Config.Layouts[guid] = new SavedLayout(name, layout);
        if (save) {
            Plugin.Config.Save();
        }
    }

    private void ApplyAllJobGaugeVisibility(SavedLayout effectiveLayout)
    {
        if (Plugin.ClientState.LocalPlayer is null)
            return;

        var jobIndex = Plugin.ClientState.LocalPlayer!.ClassJob.GameData?.JobIndex ?? 0;
        foreach (var (kind, element) in effectiveLayout.Elements) {
            if (kind.ClassJob() is { } classJob && classJob.JobIndex == jobIndex && element[ElementComponent.Visibility]) {
                ApplyJobGaugeVisibility(kind, element);
            }
        }
    }

    private unsafe void ApplyJobGaugeVisibility(ElementKind kind, Element element)
    {
        var unitName = kind.GetJobGaugeAtkName()!;
        var unit = (AtkUnitBase*)Plugin.GameGui.GetAddonByName(unitName);
        if (unit is null)
            return;

        var visibilityMask = Util.GamepadModeActive(Plugin) ? VisibilityFlags.Gamepad : VisibilityFlags.Keyboard;
        if ((element.Visibility & visibilityMask) > 0) {
            // Reveal element.
            if (unit->UldManager.NodeListCount == 0)
                unit->UldManager.UpdateDrawNodeList();
            unit->IsVisible = true;
        } else {
            // Hide element.
            if (unit->UldManager.NodeListCount > 0)
                unit->UldManager.NodeListCount = 0;
            unit->IsVisible = false;
        }
    }

    public void Dispose()
    {
    }
}

public enum HudSlot
{
    One = 0,
    Two = 1,
    Three = 2,
    Four = 3,
}

public class Vector2<T>(T x, T y)
{
    public T X { get; set; } = x;
    public T Y { get; set; } = y;
}
