namespace Client.Models;

public class MapInfo
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset StartDateUtc { get; set; }

    public DateTimeOffset EndDateUtc { get; set; }
}
