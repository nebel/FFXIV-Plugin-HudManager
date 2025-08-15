using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using HUDManager.Configuration;
using System;

namespace HUDManager;

public sealed class Swapper : IDisposable
{
    private bool _editLock;

    public Swapper()
    {
        Service.Framework.Update += OnFrameworkUpdate;
        Service.ClientState.TerritoryChanged += OnTerritoryChange;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.ClientState.TerritoryChanged -= OnTerritoryChange;
    }

    private void OnTerritoryChange(ushort tid)
    {
        if (!Plugin.Ready) {
            return;
        }

        Plugin.Statuses.Update();
        Plugin.Statuses.SetHudLayout();
    }

    public bool SetEditLock(bool value)
    {
        if (_editLock && !value) {
            // Lock was removed, so queue a force update
            Plugin.Statuses.NeedsForceUpdate = Statuses.ForceState.EditLockRemoved;
        }
        var oldValue = _editLock;
        _editLock = value;
        return oldValue != value; // Return true if the lock value changed
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Plugin.Ready || !Plugin.Config.SwapsEnabled || _editLock || !Plugin.Config.UnderstandsRisks) {
            return;
        }

        var player = Service.ClientState.LocalPlayer;
        if (player == null) {
            return;
        }

        // Skipping due to bugs caused by HUD swaps while Character Config is open
        if (Util.IsCharacterConfigOpen()) {
            return;
        }

        // Skipping due to HUD swaps in cutscenes causing main menu to become visible
        if (Service.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            || Service.Condition[ConditionFlag.WatchingCutscene78] // Used in Dalamud's cutscene check
            || Service.Condition[ConditionFlag.BoundByDuty95] // GATE: Air Force One
            || Service.Condition[ConditionFlag.PlayingLordOfVerminion]
            || Service.Condition[ConditionFlag.BetweenAreas51]) { // Loading Lord of Verminion
            return;
        }

        var updated = Plugin.Statuses.Update()
            || Plugin.Keybinder.UpdateKeyState()
            || Plugin.Statuses.CustomConditionStatus.IsUpdated();

        updated |= CheckQoLBarConditions();

        if (updated || Plugin.Statuses.NeedsForceUpdate != Statuses.ForceState.None) {
            Plugin.Statuses.SetHudLayout();
        }
    }

    private bool CheckQoLBarConditions()
    {
        var updated = false;
        foreach (var cond in Plugin.Config.CustomConditions) {
            if (cond.ConditionType == CustomConditionType.QoLBarCondition) {
                var state = Plugin.QoLBarIpc.GetConditionChange(cond.ExternalIndex, out var oldState);
                if (state != oldState) {
                    // Plugin.Log.Warning($"changed! index={cond.ExternalIndex} old={oldState} new={state}");
                    updated = true;
                }
            }
        }
        return updated;
    }
}
