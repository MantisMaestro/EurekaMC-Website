namespace Client.Models;

public class ServerSettings
{
    public string IP { get; set; } = string.Empty;

    public string Port { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Address => $"{IP}:{Port}";
}
