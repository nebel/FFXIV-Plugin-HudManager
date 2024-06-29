using Dalamud.Game.ClientState.Keys;
using HUDManager.Configuration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace HUDManager;

public class Keybinder
{
    public readonly ReadOnlyCollection<VirtualKey> ModifierKeys = new List<VirtualKey>
        {  VirtualKey.CONTROL, VirtualKey.SHIFT, VirtualKey.MENU, VirtualKey.NO_KEY }.AsReadOnly();

    public readonly ReadOnlyCollection<VirtualKey> InputKeys;

    private Dictionary<VirtualKey, bool> InputKeyState { get; } = new();

    private readonly Plugin _plugin;

    public Keybinder(Plugin plugin)
    {
        _plugin = plugin;

        InputKeys = _plugin.KeyState.GetValidVirtualKeys().ToList()
            .Except(ModifierKeys)
            .Prepend(VirtualKey.NO_KEY)
            .ToImmutableArray()
            .ToList().AsReadOnly();
    }

    public bool UpdateKeyState()
    {
        // Returns true if there's a change.
        bool TrySaveKeyState(VirtualKey? key)
        {
            if (key is not null && key.Value != VirtualKey.NO_KEY) {
                var unchanged = InputKeyState.GetValueOrDefault(key.Value) == _plugin.KeyState[key.Value];
                InputKeyState[key.Value] = _plugin.KeyState[key.Value];
                return !unchanged;
            }
            return false;
        }

        var changed = false;
        foreach (var cond in _plugin.Config.CustomConditions) {
            if (cond.ConditionType != CustomConditionType.HoldToActivate)
                continue;

            changed |= TrySaveKeyState(cond.ModifierKeyCode);
            changed |= TrySaveKeyState(cond.KeyCode);
        }

        return changed;
    }

    public bool KeybindIsPressed(VirtualKey key, VirtualKey modifier)
    {
        bool GetKeyState(VirtualKey k)
            => k == VirtualKey.NO_KEY || _plugin.KeyState[k];

        // If both keys are NO_KEY then it should be unpressable.
        if (key == VirtualKey.NO_KEY && modifier == VirtualKey.NO_KEY)
            return false;
        if (key != VirtualKey.NO_KEY && modifier != VirtualKey.NO_KEY)
            return GetKeyState(key) && GetKeyState(modifier);
        if (key == VirtualKey.NO_KEY)
            return GetKeyState(modifier);
        else
            return GetKeyState(key);
    }
}
