namespace AnalogClock;

public class ClockTheme
{
    public string Name { get; set; } = "Standard";
    public string FaceColor { get; set; } = "#FF2D2D2D";
    public string BorderColor { get; set; } = "#FF000000";
    public string NumberColor { get; set; } = "#FFFFFFFF";
    public string HourHandColor { get; set; } = "#FFFFFFFF";
    public string MinuteHandColor { get; set; } = "#FFFFFFFF";
    public string SecondHandColor { get; set; } = "#FFFFFFFF";
    public string TickColor { get; set; } = "#DDFFFFFF";
    public string GripColor { get; set; } = "#33FFFFFF";
    public string FontName { get; set; } = string.Empty;
    public bool SecondHandVisible { get; set; } = true;

    public ClockTheme Clone()
    {
        return (ClockTheme)MemberwiseClone();
    }
}
