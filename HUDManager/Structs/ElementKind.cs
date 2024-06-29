using Dalamud.Plugin.Services;
using HUDManager.Sheets;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager.Structs;

// Enum values are calculated by the game using CRC32($"{addonName}_a")
public enum ElementKind : uint
{
    // @formatter:off
    FocusTargetBar                    = 0xC292F05F,
    StatusInfoEnfeeblements           = 0x1E805A83,
    StatusInfoEnhancements            = 0x1F4230B4,
    StatusInfoConditionalEnhancements = 0x1D048EED,
    StatusInfoOther                   = 0x1CC6E4DA,
    TargetInfoHp                      = 0xBD128377,
    TargetInfoProgressBar             = 0xCB54A2EF,
    TargetInfoStatus                  = 0x076F596B,
    PartyList                         = 0x3D425039,
    EnemyList                         = 0xB8BD6685,
    ScenarioGuide                     = 0x88EE6357,
    ExperienceBar                     = 0x21E53CCE,
    PetHotbar                         = 0xD8D188FF,
    Hotbar10                          = 0xF5683FA6,
    Hotbar9                           = 0xF4AA5591,
    Hotbar8                           = 0xFFF612AC,
    Hotbar7                           = 0xFE34789B,
    Hotbar6                           = 0xFC72C6C2,
    Hotbar5                           = 0xFDB0ACF5,
    Hotbar4                           = 0xF8FFBA70,
    Hotbar3                           = 0xF93DD047,
    Hotbar2                           = 0xFB7B6E1E,
    Hotbar1                           = 0xC48D3605,
    CrossHotbar                       = 0xBA81E8D1,
    ProgressBar                       = 0xECB29811,
    Minimap                           = 0x7159021B,
    BloodGauge                        = 0xF04E8778, // DRK
    DarksideGauge                     = 0xF18CED4F, // DRK
    OathGauge                         = 4022009408, // PLD JobHudPLD0_a
    BeastGauge                        = 0x7F5D020A, // WAR
    PowderGauge                       = 0xAEC2C0DF, // GNB
    ArcanaGauge                       = 0x959978B2, // AST
    AetherflowGaugeSch                = 0xCADD58CB, // SCH
    FaerieGauge                       = 0xA1A8A487, // SCH
    HealingGauge                      = 0x7A3727B2, // WHM
    DragonGauge                       = 0xBA9838C0, // DRG
    MastersGauge                      = 0x7251AC33, // MNK
    ChakraGauge                       = 0x7393C604, // MNK
    HutonGauge                        = 0x70F99888, // NIN JobHudNIN1_a (removed in 7.0)
    Kazematoi                         = 0x6CD4313E, // NIN JobHudNIN1v70_a
    NinkiGauge                        = 0x713BF2BF, // NIN
    SenGauge                          = 0xED746DE2, // SAM
    KenkiGauge                        = 0xECB607D5, // SAM
    SongGauge                         = 0x7E747433, // BRD
    HeatGauge                         = 0x9874C76C, // MCH
    StepGauge                         = 0x90EAD514, // DNC
    FourfoldFeathers                  = 0x9128BF23, // DNC
    ElementalGauge                    = 0xDCAC125A, // BLM
    BalanceGauge                      = 0xEF0A5B00, // RDM
    TranceGauge                       = 0x3A312F0D, // SMN
    AetherflowGaugeSmn                = 0x3BF3453A, // SMN
    AddersgallGauge                   = 0x1012767E, // SGE
    EukrasiaGauge                     = 0x11D01C49, // SGE
    SoulGauge                         = 0xA2D9B660, // RPR
    DeathGauge                        = 0xA31BDC57, // RPR
    ItemHelp                          = 0x42CBE75F,
    ActionHelp                        = 0x4661EACA,
    Gil                               = 0x43161AA2,
    InventoryGrid                     = 0x1C15E20F,
    MainMenu                          = 0x8AF95A70,
    Notices                           = 0xDF217364,
    ParameterBar                      = 0x981EC49E,
    LimitGauge                        = 0xC79F450A,
    DutyList                          = 0xA29100D2,
    ServerInfo                        = 0xCDA89776,
    AllianceList1                     = 0x2943729A,
    AllianceList2                     = 0x2B05CCC3,
    NewGamePlusGuide                  = 0xDA29B46A,
    TargetBar                         = 0x913EC97D,
    StatusEffects                     = 0x4A569616,
    DutyGauge                         = 0x81394395,
    DutyAction                        = 0x54B8C68A,
    CompressedAether                  = 0xC4B6FB74,
    RivalWingsMercenaryInfo           = 0x79E67C08,
    RivalWingsTeamInfo                = 0x465E2306,
    RivalWingsStationInfo             = 0xA1173246,
    RivalWingsAllianceList            = 0xE69D30D2,
    RivalWingsGauges                  = 0x1047F0E1,
    TheFeastEnemyInfo                 = 0x366A4D0B,
    TheFeastAllyInfo                  = 0x37A8273C,
    TheFeastScore                     = 0xD7F058DF,
    BattleHighGauge                   = 0x34BF98AF,
    LeftWCrossHotbar                  = 0x6665735D,
    RightWCrossHotbar                 = 0x70DDFD27,
    OceanFishingVoyageMissions        = 0xB6D09C70,
    Timers                            = 0x99B6AD5B,
    CrystallineConflictAllyInfo       = 0xE155E172,
    CrystallineConflictBattleLog      = 0xF347F7E3,
    CrystallineConflictMap            = 0xC3ACB5D2,
    CrystallineConflictEnemyInfo      = 0xE2D1351C,
    CrystallineConflictProgressGauge  = 0x30748231,
    FrontlineScoreInfo                = 0x2D327D8E,
    BlundervilleObjective             = 0x3B26DB7A,
    BlundervilleScore                 = 0xDBC09DEA,
    BlundervilleStatus                = 0x9F0BB04E,
    BlundervilleShowLog               = 0xD83AAFFA,
    AstralGauge                       = 0xDD6E786D, // BLM JobHudBLM1_a
    Vipersight                        = 0xB7694B56, // VIP JobHudRDB0_a
    SerpentOfferingsGauge             = 0xB6AB2161, // VIP JobHudRDB1_a
    Canvases                          = 0x7A6A6A42, // PCT JobHudRPM0_a
    PaletteGauge                      = 0x7BA80075, // PCT JobHudRPM1_a
    // @formatter:on
}

public static class ElementKindExt
{
    private static FrozenDictionary<ElementKind, ClassJob> _gaugeJobs = null!;

    public static void Initialize(IDataManager data)
    {
        Dictionary<ElementKind, ClassJob> gaugeJobs = new();

        var sheet = data.GetExcelSheet<ClassJob>()!;
        foreach (var e in All()) {
            if (e.ClassJob(sheet) is { } classJob) {
                gaugeJobs[e] = classJob;
            }
        }
        _gaugeJobs = new Dictionary<ElementKind, ClassJob>(gaugeJobs).ToFrozenDictionary();
    }

    public static readonly ElementKind[] Immutable =
    [
        // don't actually know if this is immutable, but idk what it is
        ElementKind.Timers,
    ];

    public static IEnumerable<ElementKind> All() => Enum.GetValues<ElementKind>()
        .Where(kind => !Immutable.Contains(kind));

    private static int ElementKindRowId(this ElementKind kind)
    {
        return kind switch
        {
            ElementKind.Hotbar1 => 0,
            ElementKind.Hotbar2 => 1,
            ElementKind.Hotbar3 => 2,
            ElementKind.Hotbar4 => 3,
            ElementKind.Hotbar5 => 4,
            ElementKind.Hotbar6 => 5,
            ElementKind.Hotbar7 => 6,
            ElementKind.Hotbar8 => 7,
            ElementKind.Hotbar9 => 8,
            ElementKind.Hotbar10 => 9,
            ElementKind.PetHotbar => 10,
            ElementKind.CrossHotbar => 11,
            ElementKind.ProgressBar => 12,
            ElementKind.TargetBar => 13,
            ElementKind.FocusTargetBar => 14,
            ElementKind.PartyList => 15,
            ElementKind.EnemyList => 16,
            ElementKind.ParameterBar => 17,
            ElementKind.Notices => 18,
            ElementKind.Minimap => 19,
            ElementKind.MainMenu => 20,
            ElementKind.ServerInfo => 21,
            ElementKind.Gil => 22,
            ElementKind.InventoryGrid => 23,
            ElementKind.DutyList => 24,
            ElementKind.ItemHelp => 25,
            ElementKind.ActionHelp => 26,
            ElementKind.LimitGauge => 27,
            ElementKind.ExperienceBar => 28,
            ElementKind.StatusEffects => 29,
            ElementKind.AllianceList1 => 30,
            ElementKind.AllianceList2 => 31,
            // ElementKind.DutyList => 32, // Listed twice, here and at 24. Not sure if this one is ever used?
            ElementKind.Timers => 33,
            // 34-37 empty
            ElementKind.LeftWCrossHotbar => 38,
            ElementKind.RightWCrossHotbar => 39,
            ElementKind.OathGauge => 40,
            // ElementKind.LightningGauge => 41, // Discontinued monk gauge
            ElementKind.BeastGauge => 42,
            ElementKind.DragonGauge => 43,
            ElementKind.SongGauge => 44,
            ElementKind.HealingGauge => 45,
            ElementKind.ElementalGauge => 46,
            ElementKind.AetherflowGaugeSch => 47, // order? - same name, so not sure which key is for which
            ElementKind.AetherflowGaugeSmn => 48, // order?
            ElementKind.TranceGauge => 49,
            ElementKind.FaerieGauge => 50,
            ElementKind.NinkiGauge => 51,
            ElementKind.HeatGauge => 52,
            // 53 empty
            ElementKind.BloodGauge => 54,
            ElementKind.ArcanaGauge => 55,
            ElementKind.KenkiGauge => 56,
            ElementKind.SenGauge => 57,
            ElementKind.BalanceGauge => 58,
            ElementKind.DutyGauge => 59,
            ElementKind.DutyAction => 60,
            ElementKind.ChakraGauge => 61,
            ElementKind.Kazematoi => 62, // Was Huton prior to 7.0
            ElementKind.ScenarioGuide => 63,
            ElementKind.RivalWingsGauges => 64,
            ElementKind.RivalWingsAllianceList => 65,
            ElementKind.RivalWingsTeamInfo => 66,
            ElementKind.StatusInfoEnhancements => 67,
            ElementKind.StatusInfoEnfeeblements => 68,
            ElementKind.StatusInfoOther => 69,
            ElementKind.TargetInfoStatus => 70,
            ElementKind.TargetInfoProgressBar => 71,
            ElementKind.TargetInfoHp => 72,
            ElementKind.TheFeastScore => 73,
            ElementKind.TheFeastAllyInfo => 74,
            ElementKind.TheFeastEnemyInfo => 75,
            ElementKind.RivalWingsStationInfo => 76,
            ElementKind.RivalWingsMercenaryInfo => 77,
            ElementKind.DarksideGauge => 78,
            ElementKind.PowderGauge => 79,
            ElementKind.StepGauge => 80,
            ElementKind.FourfoldFeathers => 81,
            ElementKind.BattleHighGauge => 82,
            ElementKind.NewGamePlusGuide => 83,
            ElementKind.CompressedAether => 84,
            ElementKind.OceanFishingVoyageMissions => 85,
            ElementKind.StatusInfoConditionalEnhancements => 86,
            ElementKind.SoulGauge => 87,
            ElementKind.DeathGauge => 88,
            ElementKind.EukrasiaGauge => 89,
            ElementKind.AddersgallGauge => 90,
            ElementKind.MastersGauge => 91,
            ElementKind.CrystallineConflictProgressGauge => 92,
            ElementKind.CrystallineConflictAllyInfo => 93,
            // 94 empty
            ElementKind.CrystallineConflictEnemyInfo => 95,
            ElementKind.CrystallineConflictBattleLog => 96,
            ElementKind.CrystallineConflictMap => 97,
            ElementKind.FrontlineScoreInfo => 98,
            ElementKind.BlundervilleObjective => 99,
            ElementKind.BlundervilleScore => 100,
            ElementKind.BlundervilleStatus => 101,
            ElementKind.BlundervilleShowLog => 102,
            ElementKind.AstralGauge => 103,
            ElementKind.Vipersight => 104,
            ElementKind.SerpentOfferingsGauge => 105,
            ElementKind.Canvases => 106,
            ElementKind.PaletteGauge => 107,
            _ => -1,
        };
    }

    public static bool IsRealElement(this ElementKind kind)
    {
        return kind.ElementKindRowId() >= 0;
    }

    public static string LocalisedName(this ElementKind kind, IDataManager data)
    {
        var id = kind.ElementKindRowId();
        if (id < 0) {
            return kind.ToString();
        }

        var name = data.GetExcelSheet<HudSheet>()!.GetRow((uint)id)?.Name ?? kind.ToString();

        var classJob = kind.ClassJob();
        if (classJob != null) {
            name += $" ({classJob.Abbreviation})";
        }

        return name;
    }

    public static ClassJob? ClassJob(this ElementKind kind)
    {
        return _gaugeJobs.GetValueOrDefault(kind);
    }

    private static ClassJob? ClassJob(this ElementKind kind, ExcelSheet<ClassJob> sheet)
    {
        return kind switch
        {
            ElementKind.OathGauge => FindClassJob(1),
            ElementKind.ChakraGauge or ElementKind.MastersGauge => FindClassJob(2),
            ElementKind.BeastGauge => FindClassJob(3),
            ElementKind.DragonGauge => FindClassJob(4),
            ElementKind.SongGauge => FindClassJob(5),
            ElementKind.HealingGauge => FindClassJob(6),
            ElementKind.ElementalGauge or ElementKind.AstralGauge => FindClassJob(7),
            ElementKind.AetherflowGaugeSmn or ElementKind.TranceGauge => FindClassJob(8),
            ElementKind.AetherflowGaugeSch or ElementKind.FaerieGauge => FindClassJob(9),
            ElementKind.HutonGauge or ElementKind.Kazematoi or ElementKind.NinkiGauge => FindClassJob(10),
            ElementKind.HeatGauge => FindClassJob(11),
            ElementKind.BloodGauge or ElementKind.DarksideGauge => FindClassJob(12),
            ElementKind.ArcanaGauge => FindClassJob(13),
            ElementKind.KenkiGauge or ElementKind.SenGauge => FindClassJob(14),
            ElementKind.BalanceGauge => FindClassJob(15),
            ElementKind.PowderGauge => FindClassJob(17),
            ElementKind.FourfoldFeathers or ElementKind.StepGauge => FindClassJob(18),
            ElementKind.SoulGauge or ElementKind.DeathGauge => FindClassJob(19),
            ElementKind.AddersgallGauge or ElementKind.EukrasiaGauge => FindClassJob(20),
            ElementKind.Vipersight or ElementKind.SerpentOfferingsGauge => FindClassJob(21),
            ElementKind.Canvases or ElementKind.PaletteGauge => FindClassJob(22),
            _ => null,
        };

        ClassJob FindClassJob(int id)
        {
            return sheet.First(job => job.JobIndex == id);
        }
    }

    public static string? GetJobGaugeAtkName(this ElementKind kind)
    {
        return kind switch
        {
            // @formatter:off
            ElementKind.OathGauge             => "JobHudPLD0",
            ElementKind.ChakraGauge           => "JobHudMNK0",
            ElementKind.MastersGauge          => "JobHudMNK0",
            ElementKind.BeastGauge            => "JobHudWAR0",
            ElementKind.DragonGauge           => "JobHudDRG0",
            ElementKind.SongGauge             => "JobHudBRD0",
            ElementKind.HealingGauge          => "JobHudWHM0",
            ElementKind.ElementalGauge        => "JobHudBLM0",
            ElementKind.AstralGauge           => "JobHudBLM1",
            ElementKind.AetherflowGaugeSmn    => "JobHudSMN0",
            ElementKind.TranceGauge           => "JobHudSMN1",
            ElementKind.AetherflowGaugeSch    => "JobHudSCH0",
            ElementKind.FaerieGauge           => "JobHudSCH1",
            ElementKind.NinkiGauge            => "JobHudNIN0",
            ElementKind.HutonGauge            => "JobHudNIN1",
            ElementKind.Kazematoi             => "JobHudNIN1v70",
            ElementKind.HeatGauge             => "JobHudMCH0",
            ElementKind.BloodGauge            => "JobHudDRK0",
            ElementKind.DarksideGauge         => "JobHudDRK1",
            ElementKind.ArcanaGauge           => "JobHudAST0",
            ElementKind.KenkiGauge            => "JobHudSAM1",
            ElementKind.SenGauge              => "JobHudSAM0",
            ElementKind.BalanceGauge          => "JobHudRDM0",
            ElementKind.PowderGauge           => "JobHudGNB0",
            ElementKind.StepGauge             => "JobHudDNC0",
            ElementKind.FourfoldFeathers      => "JobHudDNC1",
            ElementKind.DeathGauge            => "JobHudRRP0",
            ElementKind.SoulGauge             => "JobHudRRP1",
            ElementKind.EukrasiaGauge         => "JobHudGFF0",
            ElementKind.AddersgallGauge       => "JobHudGFF1",
            ElementKind.Vipersight            => "JobHudRDB0",
            ElementKind.SerpentOfferingsGauge => "JobHudRDB1",
            ElementKind.Canvases              => "JobHudRPM0",
            ElementKind.PaletteGauge          => "JobHudRPM1",
            _ => null,
            // @formatter:on
        };
    }

    public static bool IsHotbar(this ElementKind kind)
    {
        switch (kind) {
            case ElementKind.Hotbar1:
            case ElementKind.Hotbar2:
            case ElementKind.Hotbar3:
            case ElementKind.Hotbar4:
            case ElementKind.Hotbar5:
            case ElementKind.Hotbar6:
            case ElementKind.Hotbar7:
            case ElementKind.Hotbar8:
            case ElementKind.Hotbar9:
            case ElementKind.Hotbar10:
            case ElementKind.PetHotbar:
                return true;
            default:
                return false;
        }
    }
}
