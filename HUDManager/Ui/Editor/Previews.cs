using HUD_Manager.Structs;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace HUD_Manager.Ui.Editor
{
    public class Previews
    {
        private Plugin Plugin { get; }
        private Interface Ui { get; }

        internal HashSet<ElementKind> Elements { get; } = new();
        internal HashSet<ElementKind> Update { get; } = new();

        public Previews(Plugin plugin, Interface ui)
        {
            this.Plugin = plugin;
            this.Ui = ui;
        }

        public void Draw(ref bool update)
        {
            const float tolerance = 0.0001f;
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                           | ImGuiWindowFlags.NoResize
                                           | ImGuiWindowFlags.NoFocusOnAppearing
                                           | ImGuiWindowFlags.NoScrollbar;

            if (this.Ui.SelectedLayout == Guid.Empty) {
                return;
            }

            if (!this.Plugin.Config.Layouts.TryGetValue(this.Ui.SelectedLayout, out var layout)) {
                return;
            }

            foreach (var element in layout.Elements.Values) {
                if (!this.Elements.Contains(element.Id)) {
                    continue;
                }

                var (outer, inner) = ImGuiExt.ConvertGameToImGui(element);
                var (pos, size) = outer;

                var existed = this.Update.Remove(element.Id);
                if (existed) {
                    ImGui.SetNextWindowPos(pos);
                } else {
                    ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
                }

                ImGui.SetNextWindowSize(size);
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, .5f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
                if (!ImGui.Begin($"##uimanager-preview-{element.Id}", flags)) {
                    continue;
                }
                ImGui.PopStyleVar(3);
                ImGui.PopStyleColor();

                ImGui.TextUnformatted(element.Id.LocalisedName(this.Plugin.DataManager));

                // determine if the window has moved and update if it has
                // var newPos = ImGuiExt.ConvertImGuiToGame(element, ImGui.GetWindowPos());
                // if (Math.Abs(newPos.X - element.X) > tolerance || Math.Abs(newPos.Y - element.Y) > tolerance) {
                //     element.X = newPos.X;
                //     element.Y = newPos.Y;
                //     update = true;
                // }

                if (inner != null) {
                    var drawList = ImGui.GetWindowDrawList();
                    var (innerPos, innerSize) = inner;
                    drawList.AddRectFilled(innerPos, innerPos + innerSize, 0x40FFFFFF);
                }

                ImGui.End();

                // var outer2 = ImGuiExt.ConvertGameToImGuiNested(element);
                // var (pos2, size2) = outer2;
                //
                // if (existed) {
                //     ImGui.SetNextWindowPos(pos2);
                // } else {
                //     ImGui.SetNextWindowPos(pos2, ImGuiCond.Appearing);
                // }
                //
                // ImGui.SetNextWindowSize(size2);
                // ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0f, 0f, .5f));
                // if (!ImGui.Begin($"##uimanager-preview-{element.Id}-alt", flags)) {
                //     continue;
                // }
                // ImGui.PopStyleColor();
                //
                // ImGui.TextUnformatted(element.Id.LocalisedName(this.Plugin.DataManager));
                //
                // ImGui.End();
            }
        }

        public void Clear()
        {
            Elements.Clear();
            Update.Clear();
        }
    }
}
