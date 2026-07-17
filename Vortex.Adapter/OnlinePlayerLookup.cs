using TShockAPI;

namespace Vortex.Adapter;

internal static class OnlinePlayerLookup
{
    public static TSPlayer? Find(int playerIndex, string playerName)
    {
        var player = playerIndex >= 0
            ? TShock.Players.ElementAtOrDefault(playerIndex)
            : null;

        if (player?.Active == true
            && string.Equals(player.Name, playerName, StringComparison.OrdinalIgnoreCase))
        {
            return player;
        }

        return TShock.Players.FirstOrDefault(candidate =>
            candidate?.Active == true
            && string.Equals(candidate.Name, playerName, StringComparison.OrdinalIgnoreCase));
    }
}
