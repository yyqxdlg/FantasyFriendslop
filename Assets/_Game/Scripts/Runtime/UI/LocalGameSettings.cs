using UnityEngine;

public static class LocalGameSettings
{
    // Fixed game size: Host + 3 clients = 4 players total.
    // The Settings panel will not edit this.
    public const int MaxPeople = 4;
    public const int MaxRelayConnections = MaxPeople - 1;

    private const string PlayerNameKey = "PlayerName";
    private const string AllowSameHeroKey = "AllowSameHero";

    public static string PlayerName
    {
        get
        {
            string savedName = PlayerPrefs.GetString(PlayerNameKey, "");
            if (string.IsNullOrWhiteSpace(savedName))
            {
                savedName = GetDefaultName();
                PlayerPrefs.SetString(PlayerNameKey, savedName);
                PlayerPrefs.Save();
            }

            return savedName;
        }
        set
        {
            string safeValue = string.IsNullOrWhiteSpace(value)
                ? GetDefaultName()
                : value.Trim();

            PlayerPrefs.SetString(PlayerNameKey, safeValue);
        }
    }

    public static bool AllowSameHero
    {
        get => PlayerPrefs.GetInt(AllowSameHeroKey, 0) == 1;
        set => PlayerPrefs.SetInt(AllowSameHeroKey, value ? 1 : 0);
    }

    public static string GetDefaultName()
    {
        return "Player" + Random.Range(1000, 9999);
    }
}
