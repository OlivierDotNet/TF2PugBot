﻿using System.Text;
using TF2PugBot.Extensions;
using TF2PugBot.Types;

namespace TF2PugBot.Data;

public static class MapManager
{
    private static List<GuildMapData> _guildMapData = new List<GuildMapData>();
    private static Dictionary<ulong, IgnoredSixesMap> _trackedMapIgnore = new Dictionary<ulong, IgnoredSixesMap>();

    public static List<GuildMapData> GuildMapData
    {
        get => _guildMapData;
        set => _guildMapData = value;
    }

    public static List<SixesMap> GetGuildMaps (ulong guildId)
    {
        var guildMapData = _guildMapData.FirstOrDefault(g => g.GuildId == guildId);
        if (guildMapData is null)
        {
            _guildMapData.Add(new GuildMapData() { GuildId = guildId });
            return _guildMapData.FirstOrDefault(g => g.GuildId == guildId)!.Maps;
        }

        return guildMapData.Maps;
    }

    public static async Task<List<IgnoredSixesMap>> GetIgnoredMapsAsync (ulong guildId)
    {
        var guildMapData = _guildMapData.FirstOrDefault(g => g.GuildId == guildId);
        if (guildMapData is null)
        {
            _guildMapData.Add(new GuildMapData() { GuildId = guildId });
            return _guildMapData.FirstOrDefault(g => g.GuildId == guildId)!.IgnoredMaps;
        }

        if (_trackedMapIgnore.ContainsKey(guildId)
         && _trackedMapIgnore[guildId].Added.MinutesFromNow() >= Constants.GuildGameMinDuration)
        {
            guildMapData.IgnoredMaps.Add(_trackedMapIgnore[guildId]);
            await DataManager.SaveDbAsync(SaveType.GuildMaps);
        }
        
        return guildMapData.IgnoredMaps;
    }

    public static async Task<string> GetIgnoredMapsAsStringAsync (ulong guildId)
    {
        var ignoredMaps = await GetIgnoredMapsAsync(guildId);

        StringBuilder sb = new StringBuilder();
        foreach (var map in ignoredMaps)
        {
            sb.AppendLine($"{map} will be ignored for this spin");
        }

        return sb.ToString();
    }

    public static async Task ClearIgnoredMapsAsync (ulong guildId)
    {
        var data = _guildMapData.FirstOrDefault(g => g.GuildId == guildId);
        if (data is not null)
        {
            data.IgnoredMaps.RemoveAll(m => m.Added.HoursFromNow() >= data.HoursBeforeMapClear);
            await DataManager.SaveDbAsync(SaveType.GuildMaps);
        }
    }

    public static async Task AddMapToGuildMaps (ulong guildId, string mapName)
    {
        var maps = GetGuildMaps(guildId);
        maps.Add(mapName);
        await DataManager.SaveDbAsync(SaveType.GuildMaps);

    }

    public static async Task RemoveMapFromGuildMaps (ulong guildId, string mapName)
    {
        var maps = GetGuildMaps(guildId);
        maps.RemoveAll(m => m.MapName == mapName);
        await DataManager.SaveDbAsync(SaveType.GuildMaps);
    }

    public static void PrepareMapIgnore (ulong guildId, string mapName)
    {
        if (!_trackedMapIgnore.ContainsKey(guildId))
        {
            _trackedMapIgnore.Add(guildId, new IgnoredSixesMap() {MapName = mapName});
        }
        else
        {
            _trackedMapIgnore[guildId] = new IgnoredSixesMap() {MapName = mapName};
        }
    }
}