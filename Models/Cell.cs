namespace MergeCat.Models;

public class Cell
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int Index { get; set; }
    public int UnitLevel { get; set; }

    public Player Player { get; set; }
}
