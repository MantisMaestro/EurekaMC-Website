using Client.Extensions;
using Client.Models;
using EurekaDb.Context;
using EurekaDb.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Services.Data_Service;

public class DataService(EurekaContext eurekaContext) : IDataService
{
    public async Task<List<Player>> GetOnlinePlayers()
    {
        var threshold = DateTime.UtcNow.AddSeconds(-90);
        return await eurekaContext
            .Players
            .AsNoTracking()
            .Where(x => x.LastOnline >= threshold)
            .ToListAsync();
    }

    public async Task<int> GetRecentPlayerCount()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));

        return await eurekaContext
            .PlayerSessions
            .Where(x => x.Date >= date)
            .Select(x => x.PlayerId)
            .Distinct()
            .CountAsync();
    }

    public Task<List<PlayerPlaytime>> GetDayTopPlayers(int limit)
    {
        return GetTopPlayers(limit, DateOnly.FromDateTime(DateTime.Today));
    }

    public Task<List<PlayerPlaytime>> GetWeekTopPlayers(int limit)
    {
        var weekStart = DateOnly.FromDateTime(DateTime.Today).StartOfWeek(DayOfWeek.Monday);

        return GetTopPlayers(limit, weekStart);
    }

    public Task<List<PlayerPlaytime>> GetMonthTopPlayers(int limit)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return GetTopPlayers(limit, monthStart);
    }

    public Task<List<PlayerPlaytime>> GetMapTopPlayers(int limit, DateOnly currentMapStartDate)
    {
        return GetTopPlayers(limit, currentMapStartDate);
    }

    public async Task<PlayerQuery?> GetPlayerSessions(string playerName)
    {
        var player = await eurekaContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == playerName);

        if (player is null) return null;

        var playerId = player.Id;

        var startDate = DateTime.Today.AddMonths(-1);
        var startDateOnly = DateOnly.FromDateTime(startDate);

        var sessions = await eurekaContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.Date >= startDateOnly)
            .Include(x => x.Player)
            .ToListAsync();

        var totalPlaytime = sessions.Sum(x => x.TimePlayedInSession ?? 0);

        var dates = sessions.Select(x => x.Date).ToHashSet();

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

    public async Task UpdateLedger(MCStatus.Player[] playerData, int elapsedSeconds)
    {
        foreach (var player in playerData)
        {
            var playerId = player.Uuid.ToString();
            await UpdatePlayers(player.Name, playerId, elapsedSeconds);
            await UpdateSessions(playerId, elapsedSeconds);
        }

        await eurekaContext.SaveChangesAsync();
    }

    private async Task UpdatePlayers(string playerName, string playerId, int elapsedSeconds)
    {
        var player = await eurekaContext.Players
            .FirstOrDefaultAsync(x => x.Id == playerId);

        if (player is null)
        {
            await eurekaContext.AddAsync(new Player
            {
                Id = playerId,
                Name = playerName,
                LastOnline = DateTime.UtcNow,
                TotalPlayTime = elapsedSeconds
            });
        }
        else
        {
            player.LastOnline = DateTime.UtcNow;
            player.Name = playerName;
            player.TotalPlayTime = (player.TotalPlayTime ?? 0) + elapsedSeconds;
        }
    }

    private async Task UpdateSessions(string playerId, int elapsedSeconds)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var session = await eurekaContext
            .PlayerSessions
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.Date == today);

        if (session is null)
        {
            await eurekaContext.PlayerSessions.AddAsync(new PlayerSession
            {
                PlayerId = playerId,
                Date = today,
                TimePlayedInSession = elapsedSeconds
            });
        }
        else
        {
            session.TimePlayedInSession = (session.TimePlayedInSession ?? 0) + elapsedSeconds;
        }
    }

    private async Task<List<PlayerPlaytime>> GetTopPlayers(int limit, DateOnly startDate)
    {
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        return await eurekaContext.PlayerSessions
            .Where(x => x.Date >= startDate && x.Date <= endDate)
            .GroupBy(x => new { x.PlayerId, x.Player.Name })
            .Select(g => new PlayerPlaytime
            {
                PlayerId = g.Key.PlayerId,
                PlayerName = g.Key.Name,
                Playtime = g.Sum(x => x.TimePlayedInSession ?? 0)
            })
            .OrderByDescending(x => x.Playtime)
            .Take(limit)
            .ToListAsync();
    }
}