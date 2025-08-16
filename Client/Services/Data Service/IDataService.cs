using Client.Models;
using EurekaDb.Migrations;

namespace Client.Services.Data_Service;

public interface IDataService
{
    public Task<List<Player>> GetOnlinePlayers();

    public Task<int> GetTodayPlayerCount();

    public Task<List<PlayerPlaytime>> GetDayTopPlayers(int limit);

    public Task<List<PlayerPlaytime>> GetWeekTopPlayers(int limit);

    public Task<List<PlayerPlaytime>> GetMonthTopPlayers(int limit);

    public Task<List<PlayerPlaytime>> GetMapTopPlayers(int limit);

    public Task<PlayerQuery?> GetPlayerSessions(string playerName);

    public Task UpdateLedger(string[] playerData);

    public Task UpdatePlayers(string playerName, string playerId);

    public Task UpdateSessions(string playerName, string playerId);
}