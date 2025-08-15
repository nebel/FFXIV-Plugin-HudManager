using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HUDManager.Structs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace HUDManager.Ui;

#if DEBUG
public class Debug
{
#if READONLY
    private const bool IsReadOnly = true;
#else
    private const bool IsReadOnly = false;
#endif
    private Layout? PreviousLayout { get; set; }

    private (bool drawUnknownIds, bool _) _ui = (false, false);

    internal unsafe void Draw()
    {
        if (!ImGui.BeginTabItem("Debug")) {
            return;
        }

        Header("Info");

        ImGui.Text($"Layout mode: {(IsReadOnly ? "READ_ONLY" : "READ_WRITE")}");
        ImGui.Text($"Current slot: {Hud.GetActiveHudSlot()} (int = {(int)Hud.GetActiveHudSlot()})");
        ImGui.Text($"Level: {Service.ClientState.LocalPlayer?.Level} / IsInFate: {Statuses.IsInFate()} / IsLevelSynced: {Statuses.IsLevelSynced()}");

        Header("Layout pointers");

        void ButtonCopyable(string text)
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, text))
                ImGui.SetClipboardText(text);
        }

        ImGui.Text("1:");
        ImGui.SameLine();
        ButtonCopyable($"0x{(nint)Hud.GetLayoutPointer(HudSlot.One):X}##copy1");
        ImGui.Text("2:");
        ImGui.SameLine();
        ButtonCopyable($"0x{(nint)Hud.GetLayoutPointer(HudSlot.Two):X}##copy2");
        ImGui.Text("3:");
        ImGui.SameLine();
        ButtonCopyable($"0x{(nint)Hud.GetLayoutPointer(HudSlot.Three):X}##copy3");
        ImGui.Text("4:");
        ImGui.SameLine();
        ButtonCopyable($"0x{(nint)Hud.GetLayoutPointer(HudSlot.Four):X}##copy4");

        Header("Log layout to console");

        void LogLayout(HudSlot slot)
        {
            var layout = Hud.ReadLayout(slot);
            Service.Log.Info($"===== Layout START (slot={slot}) =====");
            for (var i = 0; i < layout.elements.Length; i++) {
                Service.Log.Info($"  i={i:000} {layout.elements[i]}");
            }
            Service.Log.Info("===== Layout END =====");
        }

        if (ImGui.Button("Log 1##log1")) LogLayout(HudSlot.One);
        ImGui.SameLine();
        if (ImGui.Button("Log 2##log2")) LogLayout(HudSlot.Two);
        ImGui.SameLine();
        if (ImGui.Button("Log 3##log3")) LogLayout(HudSlot.Three);
        ImGui.SameLine();
        if (ImGui.Button("Log 4##log4")) LogLayout(HudSlot.Four);

        Header("Layout debugging");

        var unknowns = GetUnknownElements();
        if (ImGui.Button("Find unknown IDs")) {
            foreach (var v in unknowns) {
                Service.Log.Information($"Unknown ID: {v.id}");
            }
        }
        ImGui.SameLine();
        ImGui.Checkbox("Draw unknown ID labels", ref _ui.drawUnknownIds);
        if (_ui.drawUnknownIds) {
            DrawUnknownIdElements(unknowns);
        }

        if (ImGui.Button("Save layout for diff")) {
            var ptr = (nint)Hud.GetLayoutPointer(Hud.GetActiveHudSlot());
            var layout = Marshal.PtrToStructure<Layout>(ptr);
            PreviousLayout = layout;
        }
        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowRight, "Show diff with saved") &&
            PreviousLayout != null) {
            var ptr = Hud.GetLayoutPointer(Hud.GetActiveHudSlot());
            var layout = Marshal.PtrToStructure<Layout>((nint)ptr);

            foreach (var prevElem in PreviousLayout.Value.elements) {
                var currElem = layout.elements.FirstOrDefault(el => el.id == prevElem.id);
                if (currElem.visibility == prevElem.visibility && !(Math.Abs(currElem.x - prevElem.x) > .01)) {
                    continue;
                }

                Service.Log.Information(currElem.id.ToString());
                Service.ChatGui.Print(currElem.id.ToString());
            }
        }

        Header("Misc");

        if (ImGui.Button("AddonConfig.Instance()")) {
            Service.Log.Info($"{(nint)AddonConfig.Instance():X}");
        }

        if (ImGui.Button("Hud.GetAddonConfigData()")) {
            Service.Log.Info($"{(nint)Hud.GetAddonConfigData():X}");
        }

        if (ImGui.Button("SystemConfig.SystemConfigBase.ConfigBase.ConfigEntry")) {
            Service.Log.Info(
                $"{(IntPtr)Framework.Instance()->SystemConfig.SystemConfigBase.ConfigBase.ConfigEntry:X}");
        }

        if (ImGui.Button("Log ClassJob dict values")) {
            var s = "";
            foreach (var row in Service.DataManager.GetExcelSheet<ClassJob>())
                s += $"[{row.RowId}] = \"{row.Abbreviation}\",\n";
            Service.Log.Info(s);
        }

        ImGui.EndTabItem();
    }

    private static void Header(string text)
    {
        ImGui.Dummy(new Vector2(0, 2f));
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudViolet)) {
            ImGui.Text(text);
        }
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));
    }

    private static unsafe List<RawElement> GetUnknownElements()
    {
        var items = new List<RawElement>();

        foreach (var hudSlot in Enum.GetValues<HudSlot>()) {
            var ptr = (nint)Hud.GetLayoutPointer(hudSlot);
            for (var i = 0; i < 92; i++) {
                var idPtr = (ptr + i * Marshal.SizeOf<RawElement>()) + 0;
                var id = Marshal.ReadInt32(idPtr);
                if (id == 0 || items.Exists(r => (uint)r.id == (uint)id))
                    continue;
                items.Add(Marshal.PtrToStructure<RawElement>(idPtr));
            }
        }

        return items.Where(e => !Enum.IsDefined(e.id)).ToList();
    }

    private void DrawUnknownIdElements(IEnumerable<RawElement> list)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                       | ImGuiWindowFlags.NoResize
                                       | ImGuiWindowFlags.NoFocusOnAppearing
                                       | ImGuiWindowFlags.NoScrollbar
                                       | ImGuiWindowFlags.NoMove;

        foreach (var raw in list) {
            var element = new Element(raw);
            var pos = ImGuiExt.ConvertGameToImGui(element);
            ImGui.SetNextWindowPos(pos.Outer.Item1, ImGuiCond.Appearing);

            ImGui.SetNextWindowSize(pos.Outer.Item2);

            if (!ImGui.Begin($"##uimanager-preview-{element.Id}", flags)) {
                continue;
            }

            ImGui.TextUnformatted(element.Id.LocalisedName());

            ImGui.End();
        }
    }
}
#endif