namespace PingIt;

internal enum TextSize
{
    Small = 0,
    Medium = 1,
    Large = 2
}

internal static class TextSizeExtensions
{
    public static float ToFontSize(this TextSize size) => size switch
    {
        TextSize.Small => 8.5f,
        TextSize.Large => 12.5f,
        _ => 10f
    };

    public static int ToOverlayWidth(this TextSize size) => size switch
    {
        TextSize.Small => 128,
        TextSize.Large => 178,
        _ => 152
    };
}
