namespace HUD_Manager.Structs;

public record NestedNodeOffsets(int OffsetX, int OffsetY, int Width, int Height, float HeightScale)
{
    public static NestedNodeOffsets? ForElement(Element element)
    {
        return element.Id switch
        {
            ElementKind.TargetInfoProgressBar => new NestedNodeOffsets(246, 10, 204, 24, 1),
            ElementKind.TargetInfoStatus => new NestedNodeOffsets(13, 45, 375, 82, 1),
            // ElementKind.TargetInfoHp => new NestedFrame(0, 0, -1, -1, 0.5f),
            ElementKind.TargetInfoHp => new NestedNodeOffsets(0, 0, -1, 76, 0.5f),
            _ => null
        };
    }
}