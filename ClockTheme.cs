namespace AnalogClock;

public class ClockTheme
{
    public string Name { get; set; } = "Standard";
    public string FaceColor { get; set; } = "#ff0d0d0d";
    public string BorderColor { get; set; } = "#ff050505";
    public string NumberColor { get; set; } = "#ffb0b0b0";
    public string HourHandColor { get; set; } = "#ffb0b0b0";
    public string MinuteHandColor { get; set; } = "#ffb0b0b0";
    public string SecondHandColor { get; set; } = "#ff800020";
    public string TickColor { get; set; } = "#ddffffff";
    public string GripColor { get; set; } = "#33ffffff";
    public string FontName { get; set; } = "Segoe UI";
    public bool SecondHandVisible { get; set; } = true;

    public ClockTheme Clone()
    {
        return (ClockTheme)MemberwiseClone();
    }
}
