using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HUDManager.Structs;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HUDManager.Ui;

#if DEBUG
public class Debug
{
    private Layout? PreviousLayout { get; set; }

    private (bool drawUnknownIds, bool _) _ui = (false, false);

    internal void Draw()
    {
        if (!ImGui.BeginTabItem("Debug")) {
            return;
        }

        ImGui.TextUnformatted("Print layout pointer address");

        if (ImGui.Button("1##ptr1")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.One);
            Service.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("2##ptr2")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.Two);
            Service.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("3##ptr3")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.Three);
            Service.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("4##ptr4")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.Four);
            Service.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("Default##ptrDefault")) {
            var ptr = Hud.GetDefaultLayoutPointer();
            Service.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.TextUnformatted("Log layout to console");
        void LogLayout(HudSlot slot)
        {
            var layout = Hud.ReadLayout(slot);
            Service.Log.Information($"===== Layout START (slot={slot}) =====");
            for (var i = 0; i < layout.elements.Length; i++) {
                Service.Log.Information($"  i={i:000} {layout.elements[i]}");
            }
            Service.Log.Information("===== Layout END =====");
        }

        if (ImGui.Button("1##print1")) {
            LogLayout(HudSlot.One);
        }
        ImGui.SameLine();
        if (ImGui.Button("2##print2")) {
            LogLayout(HudSlot.Two);
        }
        ImGui.SameLine();
        if (ImGui.Button("3##print3")) {
            LogLayout(HudSlot.Three);
        }
        ImGui.SameLine();
        if (ImGui.Button("4##print4")) {
            LogLayout(HudSlot.Four);
        }
        ImGui.SameLine();
        if (ImGui.Button("Default?##printDefault")) {
            var layout = Marshal.PtrToStructure<Layout>(Hud.GetDefaultLayoutPointer());
            Service.Log.Information($"===== Layout START (slot=DEFAULT) =====");
            for (var i = 0; i < layout.elements.Length; i++) {
                Service.Log.Information($"  i={i:000} {layout.elements[i]}");
            }
            Service.Log.Information("===== Layout END =====");
        }

        if (ImGui.Button("Data pointer")) {
            var ptr = Hud.GetDataPointer();
            Service.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        if (ImGui.Button("CS Addon Config")) {
            unsafe {
                var ptr = AddonConfig.Instance();
                Service.ChatGui.Print($"{(nint)ptr:X}");
            }
        }

        if (ImGui.Button("Save layout")) {
            var ptr = Hud.GetLayoutPointer(Hud.GetActiveHudSlot());
            var layout = Marshal.PtrToStructure<Layout>(ptr);
            PreviousLayout = layout;
        }

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

        if (ImGui.Button("Find difference") && PreviousLayout != null) {
            var ptr = Hud.GetLayoutPointer(Hud.GetActiveHudSlot());
            var layout = Marshal.PtrToStructure<Layout>(ptr);

            foreach (var prevElem in PreviousLayout.Value.elements) {
                var currElem = layout.elements.FirstOrDefault(el => el.id == prevElem.id);
                if (currElem.visibility == prevElem.visibility && !(Math.Abs(currElem.x - prevElem.x) > .01)) {
                    continue;
                }

                Service.Log.Information(currElem.id.ToString());
                Service.ChatGui.Print(currElem.id.ToString());
            }
        }

        if (ImGui.Button("Print current slot")) {
            var slot = Hud.GetActiveHudSlot();
            Service.ChatGui.Print($"{slot} ({(int)slot})");
        }

        if (ImGui.Button("Print player status address")) {
            Service.ChatGui.Print($"{Service.ClientState.LocalPlayer:X}");
        }

        if (ImGui.Button("Print Config")) {
            unsafe {
                Service.ChatGui.Print($"{(IntPtr)Framework.Instance()->SystemConfig.SystemConfigBase.ConfigBase.ConfigEntry:X}");
            }
        }

        if (ImGui.Button("FATE Status")) {
            Service.Log.Information($"Level: {Service.ClientState.LocalPlayer?.Level}");
            Service.Log.Information($"IsInFate: {Statuses.IsInFate()}");
            Service.Log.Information($"IsLevelSynced: {Statuses.IsLevelSynced()}");
        }

        if (ImGui.Button("Print ClassJob dict values")) {
            var s = "";
            foreach (var row in Service.DataManager.GetExcelSheet<ClassJob>())
                s += $"[{row.RowId}] = \"{row.Abbreviation}\",\n";
            Service.ChatGui.Print(s);
        }

        ImGui.EndTabItem();
    }

    private static List<RawElement> GetUnknownElements()
    {
        var items = new List<RawElement>();

        foreach (var hudSlot in Enum.GetValues<HudSlot>()) {
            var ptr = Hud.GetLayoutPointer(hudSlot);
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
