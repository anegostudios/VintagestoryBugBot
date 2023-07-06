namespace VintagestoryBugBot;

public record Data
{
    /// <summary>
    ///  thread id , github issue number
    /// </summary>
    public Dictionary<ulong, int> IssueMap { get; set; } = new();
}
