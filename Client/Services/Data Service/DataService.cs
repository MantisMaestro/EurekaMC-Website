using Client.Extensions;
using Client.Models;
using EurekaDb.Context;
using EurekaDb.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Client.Services.Data_Service;

public class DataService(EurekaContext eurekaContext) : IDataService
{
    public async Task<List<Player>> GetOnlinePlayers()
    {
        return await eurekaContext
            .Players
            .Where(x => x.LastOnline == "now")
            .ToListAsync();
    }

    public async Task<int> GetTodayPlayerCount()
    {
        var date = DateOnly.FromDateTime(DateTime.Today - TimeSpan.FromDays(7));
        
        return await eurekaContext
            .PlayerSessions
            .Where(x => x.Date >= date)
            .GroupBy(x => x.PlayerId)
            .CountAsync();
    }

    public async Task<List<PlayerPlaytime>> GetDayTopPlayers(int limit)
    {
        return await GetTopPlayers(limit, DateOnly.FromDateTime(DateTime.Today), null);
    }

    public async Task<List<PlayerPlaytime>> GetWeekTopPlayers(int limit = 10)
    {
        var weekStart = DateOnly.FromDateTime(DateTime.Today).StartOfWeek(DayOfWeek.Monday);

        return await GetTopPlayers(limit, weekStart, null);
    }

    public async Task<List<PlayerPlaytime>> GetMonthTopPlayers(int limit)
    {
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthStartDate = DateOnly.FromDateTime(monthStart);

        return await GetTopPlayers(limit, monthStartDate, null);
    }

    public async Task<List<PlayerPlaytime>> GetMapTopPlayers(int limit)
    {
        // TODO: Have this based on a config file or the database!
        var mapStart = new DateOnly(2024, 07, 26);
        return await GetTopPlayers(limit, mapStart, null);
    }

    public async Task<PlayerQuery?> GetPlayerSessions(string playerName)
    {
        var player = await eurekaContext.Players
            .FirstOrDefaultAsync(x => x.Name == playerName);

        if (player is null) return null;

        var playerId = player?.Id;

        var startDate = DateTime.Today.AddMonths(-1);
        var startDateOnly = DateOnly.FromDateTime(startDate);

        var sessions = await eurekaContext.PlayerSessions
            .Where(x => x.PlayerId == playerId && x.Date >= startDateOnly)
            .Include(x => x.Player)
            .ToListAsync();

        var totalPlaytime = sessions.Sum(x => x.TimePlayedInSession ?? 0);

        var dates = sessions.Select(x => x.Date).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        for (var date = startDateOnly; date < today; date = date.AddDays(1))
            if (!dates.Contains(date))
                sessions.Add(new PlayerSession
                {
                    Date = date,
                    TimePlayedInSession = 0
                });

        sessions = sessions.OrderBy(x => x.Date).ToList();

        return new PlayerQuery
        {
            PlayerSessions = sessions,
            TotalPlaytime = totalPlaytime
        };
    }

    private async Task<List<PlayerPlaytime>> GetTopPlayers(int limit, DateOnly startDate, DateOnly? endDate)
    {
        endDate ??= DateOnly.FromDateTime(DateTime.Today);

        var sessionsThisWeek = await eurekaContext
            .PlayerSessions
            .Where(x => x.Date >= startDate && x.Date <= endDate)
            .Include(x => x.Player)
            .ToListAsync();

        var groupedPlayerSessions = sessionsThisWeek.GroupBy(x => x.PlayerId);

        var result = new List<PlayerPlaytime>();
        foreach (var groupedPlayerSession in groupedPlayerSessions)
            result.Add(new PlayerPlaytime
            {
                PlayerId = groupedPlayerSession.Key,
                PlayerName = groupedPlayerSession.First().Player.Name,
                Playtime = groupedPlayerSession.Sum(x => x.TimePlayedInSession ?? 0)
            });

        result = result.OrderByDescending(x => x.Playtime).ToList();
        return result.Take(limit).ToList();
    }
}