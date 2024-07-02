﻿using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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

    public IDalamudPluginInterface Interface { get; }
    public IPluginLog Log { get; }
    public ICommandManager CommandManager { get; }
    public IDataManager DataManager { get; }
    public IClientState ClientState { get; }
    public ICondition Condition { get; }
    public IFramework Framework { get; }
    public ISigScanner SigScanner { get; }
    public IGameInteropProvider GameInteropProvider { get; }
    public IGameGui GameGui { get; }
    public IChatGui ChatGui { get; }
    public IKeyState KeyState { get; }
    public IGameConfig GameConfig { get; }

    public Swapper Swapper { get; }
    private Commands Commands { get; }

    public Interface Ui { get; }
    public Hud Hud { get; }
    public Statuses Statuses { get; }
    public Config Config { get; }
    public HelpFile Help { get; } = null!;
    public GameFunctions GameFunctions { get; }
    private PetHotbar PetHotbar { get; }
    public Keybinder Keybinder { get; }
    public QoLBarIpc QoLBarIpc { get; }

    public readonly bool Ready;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        ICommandManager commandManager,
        IDataManager dataManager,
        IClientState clientState,
        ICondition condition,
        IFramework framework,
        ISigScanner sigScanner,
        IGameInteropProvider gameInteropProvider,
        IGameGui gameGui,
        IChatGui chatGui,
        IKeyState keyState,
        IGameConfig gameConfig)
    {
        Interface = pluginInterface;
        Log = pluginLog;
        CommandManager = commandManager;
        DataManager = dataManager;
        ClientState = clientState;
        Condition = condition;
        Framework = framework;
        SigScanner = sigScanner;
        GameInteropProvider = gameInteropProvider;
        GameGui = gameGui;
        ChatGui = chatGui;
        KeyState = keyState;
        GameConfig = gameConfig;

        ClassJobCategoryIdExtensions.Initialize(this);
        ElementKindExt.Initialize(DataManager);

        Config = Migrator.LoadConfig(this);
        Config.Initialize(Interface);
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
            } else {
                Log.Warning($"Unable to find {resourceName}");
            }
        }
        catch {
            Log.Warning("Unable to read help file");
        }

        Ui = new Interface(this);
        Hud = new Hud(this);
        Statuses = new Statuses(this);
        GameFunctions = new GameFunctions(this);
        Swapper = new Swapper(this);
        Commands = new Commands(this);
        PetHotbar = new PetHotbar(this);
        Keybinder = new Keybinder(this);
        QoLBarIpc = new QoLBarIpc(this);

        if (!Config.FirstRun) {
            Ready = true;
            return;
        }

        Config.FirstRun = false;
        if (Config.Layouts.Count == 0) {
            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                Hud.ImportSlot(
                    $"Auto-import {(int)slot + 1} ({DateTime.Now.ToString(@"yyyy-MM-dd HH\:mm\:ss", CultureInfo.InvariantCulture)})", slot, false);
            }
        }

        Config.Save();

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
