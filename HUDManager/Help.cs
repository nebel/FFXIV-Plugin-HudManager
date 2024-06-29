using System;
using System.Collections.Generic;

namespace HUDManager;

[Serializable]
public class HelpFile
{
    public List<HelpEntry> Help { get; set; } = [];
}

[Serializable]
public class HelpEntry
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;

    public List<HelpEntry>? Help { get; set; } = [];
}
