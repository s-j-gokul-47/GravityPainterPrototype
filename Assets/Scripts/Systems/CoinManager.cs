using UnityEngine;

/// <summary>
/// Manages the player's total coin count via PlayerPrefs, 
/// and tracks temporary session coins during a level.
/// </summary>
public static class CoinManager
{
    private const string TotalCoinsKey = "TotalCoins";
    
    /// <summary>
    /// Coins collected in the current active level.
    /// These are discarded if the player dies or restarts.
    /// </summary>
    public static int SessionCoins { get; private set; }

    /// <summary>
    /// Gets the permanent total amount of coins the player has saved.
    /// </summary>
    public static int GetTotalCoins()
    {
        return PlayerPrefs.GetInt(TotalCoinsKey, 0);
    }

    /// <summary>
    /// Adds one coin to the current level's session.
    /// Call this when the ball touches a coin.
    /// </summary>
    public static void AddSessionCoin()
    {
        SessionCoins++;
    }

    /// <summary>
    /// Resets the session coins back to zero.
    /// Call this when the level starts or when the player dies/falls.
    /// </summary>
    public static void ResetSessionCoins()
    {
        SessionCoins = 0;
    }

    /// <summary>
    /// Permanently saves the session coins into the total count.
    /// Call this when the player successfully crosses the finish line.
    /// </summary>
    public static void CommitSessionCoins()
    {
        if (SessionCoins > 0)
        {
            int currentTotal = GetTotalCoins();
            PlayerPrefs.SetInt(TotalCoinsKey, currentTotal + SessionCoins);
            PlayerPrefs.Save();

            // Reset session coins so they aren't added again accidentally
            SessionCoins = 0;
        }
    }

    /// <summary>
    /// Attempts to spend the given amount from the total coin balance.
    /// Returns true if the player had enough coins; false otherwise.
    /// </summary>
    public static bool SpendCoins(int amount)
    {
        int current = GetTotalCoins();
        if (current < amount)
            return false;

        PlayerPrefs.SetInt(TotalCoinsKey, current - amount);
        PlayerPrefs.Save();
        return true;
    }
}
