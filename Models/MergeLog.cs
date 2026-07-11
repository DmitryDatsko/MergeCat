namespace MergeCat.Models;

public class MergeLog
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Cell CellA { get; set; }
    public Cell CellB { get; set; }
    public int ResultingLevel { get; set; }
    public DateTime Timestamp { get; set; }
}
