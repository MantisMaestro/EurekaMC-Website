using Client.Models;
using EurekaDb.Models;

namespace Client.Services.Data_Service;

public interface IDataService
{
    Task<List<Player>> GetOnlinePlayers();

    Task<int> GetRecentPlayerCount();

    Task<List<PlayerPlaytime>> GetDayTopPlayers(int limit);

    Task<List<PlayerPlaytime>> GetWeekTopPlayers(int limit);

    Task<List<PlayerPlaytime>> GetMonthTopPlayers(int limit);

    Task<List<PlayerPlaytime>> GetMapTopPlayers(int limit, DateOnly currentMapStartDate);

    Task<PlayerQuery?> GetPlayerSessions(string playerName);

    Task UpdateLedger(MCStatus.Player[] playerData, int elapsedSeconds);
}
