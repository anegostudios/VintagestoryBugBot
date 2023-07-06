// ReSharper disable InconsistentNaming
namespace VintagestoryBugBot;

public record Config
{
    public required string GH_OWNER { get; set; }
    public required string GH_NAME { get; set; }
    public required string GH_APP_KEY { get; set; }
    public required string GH_APP_ID { get; set; }
    public required long GH_INSTALL_ID { get; set; }
    public required ulong DC_GUILD_ID { get; set; }
    public required ulong DC_CHANNEL_ID { get; set; }
    public required string DC_TOKEN { get; set; }
    public required ulong[] DC_REPORTER_ROLE_IDS { get; set; }


    public void Validate()
    {
        if (string.IsNullOrEmpty(GH_APP_KEY))
        {
            throw new ArgumentException("GH_APP_KEY config option is empty or missing. Make sure it is either in a environment variable or in the config.json");
        }

        if (string.IsNullOrEmpty(GH_APP_ID))
        {
            throw new ArgumentException("GH_APP_ID config option is empty or missing. Make sure it is either in a environment variable or in the config.json");
        }

        if (string.IsNullOrEmpty(GH_OWNER))
        {
            throw new ArgumentException("GH_OWNER config option is empty or missing. Make sure it is either in a environment variable or in the config.json");
        }

        if (string.IsNullOrEmpty(GH_NAME))
        {
            throw new ArgumentException("GH_NAME config option is empty or missing. Make sure it is either in a environment variable or in the config.json");
        }

        if (GH_INSTALL_ID == 0)
        {
            throw new ArgumentException("GH_INSTALL_ID config option is missing or 0. Make sure it is either in a environment variable or in the config.json");
        }

        if (DC_GUILD_ID == 0)
        {
            throw new ArgumentException("DC_GUILD_ID config option is missing or 0. Make sure it is either in a environment variable or in the config.json");
        }

        if (DC_CHANNEL_ID == 0)
        {
            throw new ArgumentException("DC_CHANNEL_ID config option is missing or 0. Make sure it is either in a environment variable or in the config.json");
        }

        if (string.IsNullOrEmpty(DC_TOKEN))
        {
            throw new ArgumentException("DC_TOKEN config option is empty or missing. Make sure it is either in a environment variable or in the config.json");
        }

        if (DC_REPORTER_ROLE_IDS == null || DC_REPORTER_ROLE_IDS.Length == 0)
        {
            throw new ArgumentException("DC_REPORTER_ROLE_IDS config option is missing or 0. Make sure it is either in a environment variable or in the config.json");
        }
    }

    public static Config FromEnv()
    {
        var environmentVariable = Environment.GetEnvironmentVariable(nameof(DC_REPORTER_ROLE_IDS));
        var reporterRoleIds = environmentVariable?.Split(",").Select(ulong.Parse).ToArray();
        var _ = long.TryParse(Environment.GetEnvironmentVariable(nameof(GH_INSTALL_ID)), out var GH_InstallId);
        _ = ulong.TryParse(Environment.GetEnvironmentVariable(nameof(DC_GUILD_ID)), out var DC_GuildId);
        _ = ulong.TryParse(Environment.GetEnvironmentVariable(nameof(DC_CHANNEL_ID)), out var DC_ChannelId);

        return new Config
        {
            GH_OWNER = Environment.GetEnvironmentVariable(nameof(GH_OWNER))!,
            GH_NAME = Environment.GetEnvironmentVariable(nameof(GH_NAME))!,
            GH_APP_KEY = Environment.GetEnvironmentVariable(nameof(GH_APP_KEY))!,
            GH_APP_ID = Environment.GetEnvironmentVariable(nameof(GH_APP_ID))!,
            GH_INSTALL_ID = GH_InstallId,
            DC_GUILD_ID = DC_GuildId,
            DC_CHANNEL_ID = DC_ChannelId,
            DC_TOKEN = Environment.GetEnvironmentVariable(nameof(DC_TOKEN))!,
            DC_REPORTER_ROLE_IDS = reporterRoleIds!
        };
    }
}