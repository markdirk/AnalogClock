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
    public double NumberFontScale { get; set; } = 1.0;
    public string DateColor { get; set; } = "#ffb0b0b0";
    public string DateFontName { get; set; } = "Segoe UI";
    public double DateFontScale { get; set; } = 1.0;
    public string TimeColor { get; set; } = "#ffb0b0b0";
    public string TimeFontName { get; set; } = "Segoe UI";
    public double TimeFontScale { get; set; } = 1.0;
    public bool SecondHandVisible { get; set; } = true;

    public bool HandsAboveInfo { get; set; } = true;

    public string CenterDotBorderColor { get; set; } = "#ff0d0d0d";

    public string DateBoxBackgroundColor { get; set; } = "#ff0d0d0d";
    public string DateBoxBorderColor { get; set; } = "#33ffffff";
    public double DateBoxXOffset { get; set; } = 0.0;
    public double DateBoxYOffset { get; set; } = 0.0;

    public string TimeBoxBackgroundColor { get; set; } = "#ff0d0d0d";
    public string TimeBoxBorderColor { get; set; } = "#33ffffff";
    public double TimeBoxXOffset { get; set; } = 0.0;
    public double TimeBoxYOffset { get; set; } = 0.0;

    public ClockTheme Clone()
    {
        return (ClockTheme)MemberwiseClone();
    }
}
