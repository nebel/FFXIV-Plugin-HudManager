using Newtonsoft.Json;
using System;

namespace HUDManager.Configuration;

[Serializable]
public class Window
{
    public const WindowComponent AllEnabled = WindowComponent.X | WindowComponent.Y;

    public WindowComponent Enabled { get; set; } = WindowComponent.X | WindowComponent.Y;

    public Vector2<short> Position { get; set; }

    public bool this[WindowComponent component]
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

    [JsonConstructor]
    public Window(WindowComponent enabled, Vector2<short> position)
    {
        Enabled = enabled;
        Position = position;
    }

    public Window(Vector2<short> position)
    {
        Position = position;
    }

    public Window Clone()
    {
        return new Window(Enabled, new Vector2<short>(Position.X, Position.Y));
    }

    public void UpdateEnabled(Window other)
    {
        if (other[WindowComponent.X]) {
            Position.X = other.Position.X;
        }

        if (other[WindowComponent.Y]) {
            Position.Y = other.Position.Y;
        }
    }
}

[Flags]
public enum WindowComponent
{
    X = 1 << 0,
    Y = 1 << 1,
}
