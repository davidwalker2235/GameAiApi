namespace GameAiApi.Domain;

public sealed class LeaderboardEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
}
