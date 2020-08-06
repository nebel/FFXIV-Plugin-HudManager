﻿using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// TODO: Zone swaps?

namespace HudSwap {
    public class PluginUI {
        private static readonly string[] SAVED_WINDOWS = {
            "AreaMap",
            "ChatLog",
            "ChatLogPanel_0",
            "ChatLogPanel_1",
            "ChatLogPanel_2",
            "ChatLogPanel_3",
        };

        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;

        private bool _settingsVisible = false;
        public bool SettingsVisible { get => this._settingsVisible; set => this._settingsVisible = value; }

        public PluginUI(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
        }

        public void ConfigUI(object sender, EventArgs args) {
            this.SettingsVisible = true;
        }

        private string importName = "";
        private string renameName = "";
        private Guid selectedLayout = Guid.Empty;

        private string jobFilter = "";

        private static bool configErrorOpen = true;
        public static void ConfigError() {
            if (ImGui.Begin("HudSwap error", ref configErrorOpen)) {
                ImGui.Text("Could not load HudSwap configuration.");
                ImGui.Spacing();
                ImGui.Text("If you are updating from a previous version, please\ndelete your configuration file and restart the game.");

                ImGui.End();
            }
        }

        public void DrawSettings() {
            if (!this.SettingsVisible) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;

            if (ImGui.Begin("HudSwap", ref this._settingsVisible, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (ImGui.BeginTabBar("##hudswap-tabs")) {
                    if (!this.plugin.Config.UnderstandsRisks) {
                        if (ImGui.BeginTabItem("About")) {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
                            ImGui.Separator();
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                            ImGui.Text("HudSwap will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                            ImGui.Spacing();
                            ImGui.Text("Any HUD layout changes you make while HudSwap is enabled may potentially be lost, no matter what slot. If you want to make changes to your HUD layout, TURN OFF HudSwap first.");
                            ImGui.Spacing();
                            ImGui.Text("When editing or making a new layout, to be completely safe, turn off swaps, set up your layout, import the layout into HudSwap, then turn on swaps.");
                            ImGui.Spacing();
                            ImGui.Text("If you are a new user, HudSwap auto-imported your existing layouts on startup.");
                            ImGui.Spacing();
                            ImGui.Text("Finally, HudSwap is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                            ImGui.Separator();
                            ImGui.Text("If you have read all of the above and are okay with continuing, check the box below to enable HudSwap. You only need to do this once.");
                            ImGui.PopTextWrapPos();
                            bool understandsRisks = this.plugin.Config.UnderstandsRisks;
                            if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                                this.plugin.Config.UnderstandsRisks = understandsRisks;
                                this.plugin.Config.Save();
                            }

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                        ImGui.End();
                        return;
                    }

                    if (ImGui.BeginTabItem("Layouts")) {
                        ImGui.Text("Saved layouts");
                        if (this.plugin.Config.Layouts2.Count == 0) {
                            ImGui.Text("None saved!");
                        } else {
                            if (ImGui.ListBoxHeader("##saved-layouts")) {
                                foreach (KeyValuePair<Guid, Layout> entry in this.plugin.Config.Layouts2) {
                                    if (ImGui.Selectable($"{entry.Value.Name}##{entry.Key}", this.selectedLayout == entry.Key)) {
                                        this.selectedLayout = entry.Key;
                                        this.renameName = entry.Value.Name;
                                    }
                                }
                                ImGui.ListBoxFooter();
                            }

                            ImGui.Text("Copy onto slot...");
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                string buttonName = $"{(int)slot + 1}##copy";
                                if (ImGui.Button(buttonName) && this.selectedLayout != null) {
                                    Layout layout = this.plugin.Config.Layouts2[this.selectedLayout];
                                    this.plugin.Hud.WriteLayout(slot, layout.Hud.ToArray());
                                }
                                ImGui.SameLine();
                            }

                            if (ImGui.Button("Delete") && this.selectedLayout != null) {
                                this.plugin.Config.Layouts2.Remove(this.selectedLayout);
                                this.selectedLayout = Guid.Empty;
                                this.renameName = "";
                                this.plugin.Config.Save();
                            }
                            ImGui.SameLine();

                            if (ImGui.Button("Copy to clipboard") && this.selectedLayout != null) {
                                if (this.plugin.Config.Layouts2.TryGetValue(this.selectedLayout, out Layout layout)) {
                                    SharedLayout shared = new SharedLayout(layout);
                                    string json = JsonConvert.SerializeObject(shared);
                                    ImGui.SetClipboardText(json);
                                }
                            }

                            ImGui.InputText("##rename-input", ref this.renameName, 100);
                            ImGui.SameLine();
                            if (ImGui.Button("Rename") && this.renameName.Length != 0 && this.selectedLayout != null) {
                                Layout layout = this.plugin.Config.Layouts2[this.selectedLayout];
                                Layout newLayout = new Layout(this.renameName, layout.Hud, layout.Positions);
                                this.plugin.Config.Layouts2[this.selectedLayout] = newLayout;
                                this.plugin.Config.Save();
                            }
                        }

                        ImGui.Separator();

                        ImGui.Text("Import");

                        ImGui.InputText("Imported layout name", ref this.importName, 100);

                        bool importPositions = this.plugin.Config.ImportPositions;
                        if (ImGui.Checkbox("Import window positions", ref importPositions)) {
                            this.plugin.Config.ImportPositions = importPositions;
                            this.plugin.Config.Save();
                        }
                        ImGui.SameLine();
                        HelpMarker("If this is checked, the position of the chat box and the map will be saved with the imported layout.");

                        foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                            string buttonName = $"{(int)slot + 1}##import";
                            if (ImGui.Button(buttonName) && this.importName.Length != 0) {
                                this.ImportSlot(this.importName, slot);
                                this.importName = "";
                            }
                            ImGui.SameLine();
                        }

                        if (ImGui.Button("Clipboard") && this.importName.Length != 0) {
                            SharedLayout shared = null;
                            try {
                                shared = (SharedLayout)JsonConvert.DeserializeObject(ImGui.GetClipboardText(), typeof(SharedLayout));
#pragma warning disable CA1031 // Do not catch general exception types
                            } catch (Exception) {
#pragma warning restore CA1031 // Do not catch general exception types
                            }
                            if (shared != null) {
                                byte[] layout = shared.Layout();
                                if (layout != null) {
                                    this.Import(this.importName, layout, shared.Positions);
                                    this.importName = "";
                                }
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Swaps")) {
                        bool enabled = this.plugin.Config.SwapsEnabled;
                        if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                            this.plugin.Config.SwapsEnabled = enabled;
                            this.plugin.Config.Save();
                        }
                        ImGui.Text("Note: Disable swaps when editing your HUD.");

                        ImGui.Spacing();
                        string staging = ((int)this.plugin.Config.StagingSlot + 1).ToString();
                        if (ImGui.BeginCombo("Staging slot", staging)) {
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                if (ImGui.Selectable(((int)slot + 1).ToString())) {
                                    this.plugin.Config.StagingSlot = slot;
                                    this.plugin.Config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

                        ImGui.Separator();

                        ImGui.Text("This is the default layout. If none of the below conditions are\nsatisfied, this layout will be enabled.");

                        if (ImGui.BeginCombo("##default-layout", this.LayoutNameOrDefault(this.plugin.Config.DefaultLayout))) {
                            foreach (KeyValuePair<Guid, Layout> entry in this.plugin.Config.Layouts2) {
                                if (ImGui.Selectable(entry.Value.Name)) {
                                    this.plugin.Config.DefaultLayout = entry.Key;
                                    this.plugin.Config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }

                        ImGui.Spacing();
                        ImGui.Text("These settings are ordered from highest priority to lowest priority.\nHigher priorities overwrite lower priorities when enabled.");
                        ImGui.Spacing();

                        if (ImGui.CollapsingHeader("Status layouts", ImGuiTreeNodeFlags.DefaultOpen)) {
                            if (ImGui.BeginChild("##layout-selections", new Vector2(0, 125))) {
                                ImGui.Columns(2);

                                float maxSize = 0f;

                                foreach (Status status in Statuses.ORDER.Reverse()) {
                                    maxSize = Math.Max(maxSize, ImGui.CalcTextSize(status.Name()).X);

                                    this.plugin.Config.StatusLayouts.TryGetValue(status, out Guid layout);

                                    if (this.LayoutBox(status.Name(), layout, out Guid newLayout)) {
                                        this.plugin.Config.StatusLayouts[status] = newLayout;
                                        this.plugin.Config.Save();
                                        if (this.plugin.Config.SwapsEnabled) {
                                            this.plugin.Statuses.SetHudLayout(player, true);
                                        }
                                    }
                                }

                                ImGui.SetColumnWidth(0, maxSize + ImGui.GetStyle().ItemSpacing.X * 2);

                                ImGui.Columns(1);
                                ImGui.EndChild();
                            }
                        }

                        if (ImGui.CollapsingHeader("Job layouts")) {
                            if (ImGui.InputText("Filter", ref this.jobFilter, 50, ImGuiInputTextFlags.AutoSelectAll)) {
                                this.jobFilter = this.jobFilter.ToLower();
                            }

                            if (ImGui.BeginChild("##job-layout-selections", new Vector2(0, 125))) {
                                ImGui.Columns(2);

                                float maxSize = 0f;

                                var acceptableJobs = this.pi.Data.GetExcelSheet<ClassJob>()
                                    .Where(job => job.NameEnglish != "Adventurer")
                                    .Where(job => this.jobFilter.Length == 0 || (job.NameEnglish.ToLower().Contains(this.jobFilter) || job.Abbreviation.ToLower().Contains(this.jobFilter)));

                                foreach (ClassJob job in acceptableJobs) {
                                    maxSize = Math.Max(maxSize, ImGui.CalcTextSize(job.NameEnglish).X);

                                    this.plugin.Config.JobLayouts.TryGetValue(job.Abbreviation, out Guid layout);

                                    if (this.LayoutBox(job.NameEnglish, layout, out Guid newLayout)) {
                                        this.plugin.Config.JobLayouts[job.Abbreviation] = newLayout;
                                        this.plugin.Config.Save();
                                        if (this.plugin.Config.SwapsEnabled) {
                                            this.plugin.Statuses.SetHudLayout(player, true);
                                        }
                                    }
                                }

                                ImGui.SetColumnWidth(0, maxSize + ImGui.GetStyle().ItemSpacing.X * 2);

                                ImGui.Columns(1);
                                ImGui.EndChild();
                            }

                            bool combatOnlyJobs = this.plugin.Config.JobsCombatOnly;
                            if (ImGui.Checkbox("Jobs only in combat/weapon drawn", ref combatOnlyJobs)) {
                                this.plugin.Config.JobsCombatOnly = combatOnlyJobs;
                                this.plugin.Config.Save();
                                if (this.plugin.Config.SwapsEnabled) {
                                    this.plugin.Statuses.SetHudLayout(player, true);
                                }
                            }
                            ImGui.SameLine();
                            HelpMarker("Selecting this will make the HUD layout change for a job only when in combat or when your weapon is drawn.");

                            bool highPriorityJobs = this.plugin.Config.HighPriorityJobs;
                            if (ImGui.Checkbox("Jobs take priority over status", ref highPriorityJobs)) {
                                this.plugin.Config.HighPriorityJobs = highPriorityJobs;
                                this.plugin.Config.Save();
                                if (this.plugin.Config.SwapsEnabled) {
                                    this.plugin.Statuses.SetHudLayout(player, true);
                                }
                            }
                            ImGui.SameLine();
                            HelpMarker("Selecting this will make job layouts always apply when on that job. If this is unselected, job layouts will only apply if the default layout was going to be used (or only in combat if the above checkbox is selected).");
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }

        private static void HelpMarker(string text) {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        private string LayoutNameOrDefault(Guid key) {
            if (this.plugin.Config.Layouts2.TryGetValue(key, out Layout layout)) {
                return layout.Name;
            } else {
                return "";
            }
        }

        public void Draw() {
            this.DrawSettings();
        }

        private bool LayoutBox(string name, Guid currentLayout, out Guid newLayout) {
            newLayout = Guid.Empty;
            bool updated = false;
            ImGui.Text(name);
            ImGui.NextColumn();
            if (ImGui.BeginCombo($"##{name}-layout", this.LayoutNameOrDefault(currentLayout))) {
                if (ImGui.Selectable("Not set")) {
                    updated = true;
                }
                ImGui.Separator();
                foreach (KeyValuePair<Guid, Layout> entry in this.plugin.Config.Layouts2) {
                    if (ImGui.Selectable(entry.Value.Name)) {
                        updated = true;
                        newLayout = entry.Key;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.NextColumn();

            return updated;
        }

        private Dictionary<string, Vector2<short>> GetPositions() {
            Dictionary<string, Vector2<short>> positions = new Dictionary<string, Vector2<short>>();

            foreach (string name in SAVED_WINDOWS) {
                Vector2<short> pos = this.plugin.GameFunctions.GetWindowPosition(name);
                if (pos != null) {
                    positions[name] = pos;
                }
            }

            return positions;
        }

        public void ImportSlot(string name, HudSlot slot, bool save = true) {
            Dictionary<string, Vector2<short>> positions;
            if (this.plugin.Config.ImportPositions) {
                positions = this.GetPositions();
            } else {
                positions = new Dictionary<string, Vector2<short>>();
            }
            this.Import(name, this.plugin.Hud.ReadLayout(slot), positions, save);
        }

        public void Import(string name, byte[] layout, Dictionary<string, Vector2<short>> positions, bool save = true) {
            this.plugin.Config.Layouts2[Guid.NewGuid()] = new Layout(name, layout, positions);
            if (save) {
                this.plugin.Config.Save();
            }
        }
    }
}
