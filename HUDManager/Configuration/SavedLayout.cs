using HUDManager.Structs;
using HUDManager.Structs.External;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager.Configuration;

[Serializable]
public class SavedLayout
{
    public Dictionary<ElementKind, Element> Elements { get; }
    public Dictionary<string, Window> Windows { get; }

    // The original approach was to have a superclass "ExternalElement" but this causes some weird issues with deserialization, in
    //  which Dalamud would reset the BrowsingwayOverlay to an ExternalElement, wiping all the data in the process.
    public List<BrowsingwayOverlay> BrowsingwayOverlays { get; } = [];
    public CrossUpConfig? CrossUpConfig { get; set; }

    // public Dictionary<string, Vector2<short>> Positions { get; private set; }

    public Guid Parent { get; set; } = Guid.Empty;

    public string Name { get; set; }

    [JsonConstructor]
    public SavedLayout(string name, Dictionary<ElementKind, Element> elements, Dictionary<string, Window> windows, Guid parent)
    {
        Name = name;
        Elements = elements;
        Windows = windows;
        Parent = parent;
    }

    public SavedLayout(string name, Dictionary<ElementKind, Element> elements, Dictionary<string, Window> windows, List<BrowsingwayOverlay> overlays, CrossUpConfig? xup, Guid parent) : this(name, elements, windows, parent)
    {
        BrowsingwayOverlays = overlays;
        CrossUpConfig = xup;
    }

    public SavedLayout(string name, Layout hud, Dictionary<string, Window> windows)
    {
        Name = name;
        Elements = hud.ToDictionary();
        Windows = windows;
    }

    public SavedLayout(string name, Layout hud)
    {
        Name = name;
        Elements = hud.ToDictionary();
        Windows = new Dictionary<string, Window>();
    }

    public SavedLayout(SavedLayout layout)
    {
        Name = layout.Name;
        Elements = layout.Elements;
        Windows = layout.Windows;
        BrowsingwayOverlays = layout.BrowsingwayOverlays;
        CrossUpConfig = layout.CrossUpConfig;
        Parent = layout.Parent;
    }

    public Layout ToLayout()
    {
        var elements = Elements.Values.ToList();

        while (elements.Count < Hud.InMemoryLayoutElements) {
            elements.Add(new Element(new RawElement()));
        }

        return new Layout
        {
            elements = elements.Select(elem => new RawElement(elem)).ToArray(),
        };
    }
}
