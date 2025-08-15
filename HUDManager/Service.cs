using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace HUDManager;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class Service
{
    [PluginService] public static IDalamudPluginInterface Interface { get; set; }
    [PluginService] public static IPluginLog Log { get; set; }
    [PluginService] public static ICommandManager CommandManager { get; set; }
    [PluginService] public static IDataManager DataManager { get; set; }
    [PluginService] public static IClientState ClientState { get; set; }
    [PluginService] public static ICondition Condition { get; set; }
    [PluginService] public static IFramework Framework { get; set; }
    [PluginService] public static ISigScanner SigScanner { get; set; }
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; }
    [PluginService] public static IGameGui GameGui { get; set; }
    [PluginService] public static IChatGui ChatGui { get; set; }
    [PluginService] public static IKeyState KeyState { get; set; }
    [PluginService] public static IGameConfig GameConfig { get; set; }
}
