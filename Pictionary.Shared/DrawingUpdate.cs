namespace Pictionary.Shared.Models;

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class DrawingUpdate
{
    public Point From { get; set; } = null!;
    public Point To { get; set; } = null!;
    public string Color { get; set; } = "#000000";
    public int Thickness { get; set; } = 2;
}