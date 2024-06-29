using Dalamud.Plugin.Ipc;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HUDManager;

public class QoLBarIpc
{
    public const int IndexUnset = -1;
    public const int IndexRemoved = -2;

    private readonly Plugin _plugin;
    private string[] _conditionList = [];
    private Dictionary<int, ConditionState> _cache = new();

    private readonly ICallGateSubscriber<object> _qolBarInitializedSubscriber;
    private readonly ICallGateSubscriber<object> _qolBarDisposedSubscriber;
    private readonly ICallGateSubscriber<string> _qolBarGetVersionSubscriber;
    private readonly ICallGateSubscriber<int> _qolBarGetIpcVersionSubscriber;
    private readonly ICallGateSubscriber<string[]> _qolBarGetConditionSetsProvider;
    private readonly ICallGateSubscriber<int, bool> _qolBarCheckConditionSetProvider;
    private readonly ICallGateSubscriber<int, int, object> _qolBarMovedConditionSetProvider;
    private readonly ICallGateSubscriber<int, object> _qolBarRemovedConditionSetProvider;

    public bool Enabled { get; private set; }

    public string Version
    {
        get
        {
            try {
                return _qolBarGetVersionSubscriber.InvokeFunc();
            }
            catch {
                return "0.0.0.0";
            }
        }
    }

    public int IpcVersion
    {
        get
        {
            try {
                return _qolBarGetIpcVersionSubscriber.InvokeFunc();
            }
            catch {
                return 0;
            }
        }
    }

    public string[] GetConditionSets()
    {
        try {
            _conditionList = _qolBarGetConditionSetsProvider.InvokeFunc();
            return _conditionList;
        }
        catch (Exception e) {
            _plugin.Log.Warning(e, "Error fetching QoL Bar condition sets");
            return [];
        }
    }

    public ConditionState GetConditionChange(int index, out ConditionState? oldState)
    {
        if (_cache.TryGetValue(index, out var cachedState)) {
            oldState = cachedState;
        } else {
            oldState = null;
        }

        var state = GetConditionState(index);
        _cache[index] = state;
        return state;
    }

    public ConditionState GetConditionState(int index)
    {
        if (!Enabled) {
            return ConditionState.ErrorPluginUnavailable;
        }

        if (index < 0) {
            if (index == IndexRemoved) {
                return ConditionState.ErrorConditionRemoved;
            }
            return ConditionState.ErrorConditionNotSet;
        }

        if (index >= _conditionList.Length) {
            return ConditionState.ErrorConditionNotFound;
        }

        try {
            return _qolBarCheckConditionSetProvider.InvokeFunc(index) ? ConditionState.True : ConditionState.False;
        }
        catch {
            return ConditionState.ErrorUnknown;
        }
    }

    private void OnMovedConditionSet(int from, int to)
    {
        _plugin.Log.Debug($"QoL Bar conditions swapped: {from} <-> {to}");

        var changed = false;
        foreach (var condition in _plugin.Config.CustomConditions) {
            if (condition.ConditionType == CustomConditionType.QoLBarCondition) {
                if (condition.ExternalIndex == from) {
                    condition.ExternalIndex = to;
                    changed = true;
                } else if (condition.ExternalIndex == to) {
                    condition.ExternalIndex = from;
                    changed = true;
                }
            }
        }

        if (changed) {
            _plugin.Config.Save();
        }

        ClearCache();
    }

    private void OnRemovedConditionSet(int removed)
    {
        _plugin.Log.Debug($"QoL Bar condition removed: {removed}");

        var changed = false;
        foreach (var condition in _plugin.Config.CustomConditions) {
            if (condition.ConditionType == CustomConditionType.QoLBarCondition && condition.ExternalIndex == removed) {
                condition.ExternalIndex = -2;
                changed = true;
            }
        }

        if (changed) {
            _plugin.Config.Save();
        }

        ClearCache();
    }

    public void ClearCache()
    {
        _cache = new();
        if (Enabled) {
            GetConditionSets();
        } else {
            _conditionList = [];
        }
    }

    public QoLBarIpc(Plugin plugin)
    {
        _plugin = plugin;

        _qolBarInitializedSubscriber = plugin.Interface.GetIpcSubscriber<object>("QoLBar.Initialized");
        _qolBarDisposedSubscriber = plugin.Interface.GetIpcSubscriber<object>("QoLBar.Disposed");
        _qolBarGetIpcVersionSubscriber = plugin.Interface.GetIpcSubscriber<int>("QoLBar.GetIPCVersion");
        _qolBarGetVersionSubscriber = plugin.Interface.GetIpcSubscriber<string>("QoLBar.GetVersion");
        _qolBarGetConditionSetsProvider = plugin.Interface.GetIpcSubscriber<string[]>("QoLBar.GetConditionSets");
        _qolBarCheckConditionSetProvider = plugin.Interface.GetIpcSubscriber<int, bool>("QoLBar.CheckConditionSet");
        _qolBarMovedConditionSetProvider = plugin.Interface.GetIpcSubscriber<int, int, object>("QoLBar.MovedConditionSet");
        _qolBarRemovedConditionSetProvider = plugin.Interface.GetIpcSubscriber<int, object>("QoLBar.RemovedConditionSet");

        _qolBarInitializedSubscriber.Subscribe(Enable);
        _qolBarDisposedSubscriber.Subscribe(Disable);
        _qolBarMovedConditionSetProvider.Subscribe(OnMovedConditionSet);
        _qolBarRemovedConditionSetProvider.Subscribe(OnRemovedConditionSet);

        Enable();
    }

    private void Enable()
    {
        if (IpcVersion != 1) {
            return;
        }

        _plugin.Log.Debug("Enabling QoLBar IPC");
        Enabled = true;
        ClearCache();
    }

    private void Disable()
    {
        if (!Enabled) {
            return;
        }

        _plugin.Log.Debug("Disabling QoLBar IPC");
        Enabled = false;
        ClearCache();
    }

    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    public void Dispose()
    {
        Enabled = false;
        _qolBarInitializedSubscriber?.Unsubscribe(Enable);
        _qolBarDisposedSubscriber?.Unsubscribe(Disable);
        _qolBarMovedConditionSetProvider?.Unsubscribe(OnMovedConditionSet);
        _qolBarRemovedConditionSetProvider?.Unsubscribe(OnRemovedConditionSet);
    }
}

public enum ConditionState
{
    False = 0,
    True = 1,
    ErrorConditionNotSet = 2,
    ErrorPluginUnavailable = 3,
    ErrorConditionRemoved = 4,
    ErrorConditionNotFound = 5,
    ErrorUnknown = 6,
}
