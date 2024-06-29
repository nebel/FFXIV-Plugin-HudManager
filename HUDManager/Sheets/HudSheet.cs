using Lumina.Data;
using Lumina.Excel;
using Lumina.Text;
using System.Diagnostics.CodeAnalysis;

namespace HUDManager.Sheets;

[Sheet("Hud")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Used in generic parameter")]
public class HudSheet : ExcelRow
{
    public string Name { get; private set; } = null!;
    public string ShortName { get; private set; } = null!;
    public string ShorterName { get; private set; } = null!;

    public override void PopulateData(RowParser parser, Lumina.GameData lumina, Language language)
    {
        RowId = parser.RowId;
        SubRowId = parser.SubRowId;
        Name = parser.ReadColumn<SeString>(0) ?? "";
        ShortName = parser.ReadColumn<SeString>(1) ?? "";
        ShorterName = parser.ReadColumn<SeString>(2) ?? "";

        SheetLanguage = language;
        SheetName = parser.Sheet.Name;
    }
}
