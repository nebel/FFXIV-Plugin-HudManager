using System;
using System.Linq;

namespace HUDManager.Structs.External;

[Serializable]
public class BrowsingwayOverlay
{
    public const BrowsingwayOverlayComponent AllEnabled =
        BrowsingwayOverlayComponent.Hidden
        | BrowsingwayOverlayComponent.Locked
        | BrowsingwayOverlayComponent.Typethrough
        | BrowsingwayOverlayComponent.Clickthrough;

    public BrowsingwayOverlayComponent Enabled { get; set; } = BrowsingwayOverlayComponent.Hidden;

    public string CommandName = string.Empty;

    public bool Hidden;
    public bool Locked;
    public bool Typethrough;
    public bool Clickthrough;

    public BrowsingwayOverlay() { }

    public BrowsingwayOverlay(string name, BrowsingwayOverlayComponent enabled, bool hidden, bool locked, bool typethrough, bool clickthrough)
    {
        CommandName = name;
        Enabled = enabled;
        Hidden = hidden;
        Locked = locked;
        Typethrough = typethrough;
        Clickthrough = clickthrough;
    }

    public void ApplyOverlay(Plugin plugin)
    {
        if (CommandName == string.Empty || CommandName.Any(char.IsWhiteSpace))
            return;

        void RunCommand(string parameter, bool option)
        {
            plugin.CommandManager.ProcessCommand($"/bw inlay {CommandName} {parameter} {(option ? "on" : "off")}");
        }

        if (this[BrowsingwayOverlayComponent.Hidden]) {
            RunCommand("hidden", Hidden);
        }
        if (this[BrowsingwayOverlayComponent.Locked]) {
            RunCommand("locked", Locked);
        }
        if (this[BrowsingwayOverlayComponent.Typethrough]) {
            RunCommand("typethrough", Typethrough);
        }
        if (this[BrowsingwayOverlayComponent.Clickthrough]) {
            RunCommand("clickthrough", Clickthrough);
        }
    }

    public bool this[BrowsingwayOverlayComponent component]
    {
        get => (Enabled & component) > 0;
        set
        {
            if (value) {
                Enabled |= component;
            } else {
                Enabled &= ~component;
            }
        }
    }

    public BrowsingwayOverlay Clone()
    {
        return new BrowsingwayOverlay(CommandName, Enabled, Hidden, Locked, Typethrough, Clickthrough);
    }

    public void UpdateEnabled(BrowsingwayOverlay other)
    {
        if (other[BrowsingwayOverlayComponent.Hidden]) {
            Hidden = other.Hidden;
        }
        if (other[BrowsingwayOverlayComponent.Locked]) {
            Locked = other.Locked;
        }
        if (other[BrowsingwayOverlayComponent.Typethrough]) {
            Typethrough = other.Typethrough;
        }
        if (other[BrowsingwayOverlayComponent.Clickthrough]) {
            Clickthrough = other.Clickthrough;
        }
    }

    [Flags]
    public enum BrowsingwayOverlayComponent
    {
        Hidden = 1 << 0,
        Locked = 1 << 1,
        Typethrough = 1 << 2,
        Clickthrough = 1 << 3,
    }
}
