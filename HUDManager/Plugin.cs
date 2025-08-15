using Dalamud.Plugin;
using HUDManager.Configuration;
using HUDManager.Structs;
using HUDManager.Ui;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HUDManager;

public sealed class Plugin : IDalamudPlugin
{
    public const string Name = "HUD Manager";

    public static Swapper Swapper { get; private set; } = null!;
    private static Commands Commands { get; set; } = null!;
    public static Interface Ui { get; private set; } = null!;
    public static Hud Hud { get; private set; } = null!;
    public static Statuses Statuses { get; private set; } = null!;
    public static Config Config { get; private set; } = null!;
    public static HelpFile Help { get; private set; } = null!;
    public static GameFunctions GameFunctions { get; private set; } = null!;
    private static PetHotbar PetHotbar { get; set; } = null!;
    public static Keybinder Keybinder { get; private set; } = null!;
    public static QoLBarIpc QoLBarIpc { get; private set; } = null!;
    
    public static bool Ready { get; private set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        ClassJobCategoryIdExtensions.Initialize();
        ElementKindExt.Initialize();

        Config = Migrator.LoadConfig();
        Config.Save();

        try {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.help.yaml";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream != null) {
                using var reader = new StreamReader(stream);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                Help = deserializer.Deserialize<HelpFile>(reader);
            }
            else {
                Service.Log.Warning($"Unable to find {resourceName}");
            }
        }
        catch {
            Service.Log.Warning("Unable to read help file");
        }

        Ui = new Interface();
        Hud = new Hud();
        Statuses = new Statuses();
        GameFunctions = new GameFunctions();
        Swapper = new Swapper();
        Commands = new Commands();
        PetHotbar = new PetHotbar();
        Keybinder = new Keybinder();
        QoLBarIpc = new QoLBarIpc();

        if (Config.FirstRun) {
            Config.FirstRun = false;
            if (Config.Layouts.Count == 0) {
                foreach (var slot in Enum.GetValues<HudSlot>()) {
                    Hud.ImportSlot(
                        $"Auto-import {(int)slot + 1} ({DateTime.Now.ToString(@"yyyy-MM-dd HH\:mm\:ss", CultureInfo.InvariantCulture)})",
                        slot, false);
                }
            }
            Config.Save();
        }

        Ready = true;
    }

    public void Dispose()
    {
        Commands.Dispose();
        Ui.Dispose();
        Swapper.Dispose();
        PetHotbar.Dispose();
        Hud.Dispose();
        QoLBarIpc.Dispose();
    }
}