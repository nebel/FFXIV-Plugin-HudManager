using Dalamud.Interface.Utility;
using HUDManager.Ui.Editor;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace HUDManager.Ui;

public sealed class Interface : IDisposable
{
    private LayoutEditor LayoutEditor { get; }
    private Swaps Swaps { get; }
    private Help Help { get; }
    private FirstUseWarning FirstUseWarning { get; }
#if DEBUG
    private Debug Debug { get; }
#endif

    public Guid SelectedLayout { get; set; } = Guid.Empty;

    private bool _settingsVisible;

    private bool SettingsVisible
    {
        get => _settingsVisible;
        set => _settingsVisible = value;
    }

    public Interface()
    {
        LayoutEditor = new LayoutEditor( this);
        Swaps = new Swaps();
        Help = new Help();
        FirstUseWarning = new FirstUseWarning();
#if DEBUG
        Debug = new Debug();
#endif

        Service.Interface.UiBuilder.Draw += Draw;
        Service.Interface.UiBuilder.OpenConfigUi += OpenConfig;
    }

    public void Dispose()
    {
        Service.Interface.UiBuilder.OpenConfigUi -= OpenConfig;
        Service.Interface.UiBuilder.Draw -= Draw;
    }

    internal void OpenConfig()
    {
        SettingsVisible = true;
    }

    private void Draw()
    {
        if (!SettingsVisible) {
            return;
        }

        var update = false;

        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(530, 530), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(530, 530), new Vector2(int.MaxValue, int.MaxValue));

        var expanded = ImGui.Begin(Plugin.Name, ref _settingsVisible);
        if (!expanded || !_settingsVisible) {
            Plugin.Swapper.SetEditLock(false);
            return;
        }

        if (ImGui.BeginTabBar("##hudmanager-tabs")) {
            if (!Plugin.Config.UnderstandsRisks) {
                FirstUseWarning.Draw(ref update);
                goto End;
            }

            LayoutEditor.Draw();

            Swaps.Draw();

            Help.Draw(ref update);

#if DEBUG
            Debug.Draw();
#endif

            End:
            ImGui.EndTabBar();
        }

        if (update) {
            Plugin.Config.Save();
        }

        ImGui.End();
    }
}
