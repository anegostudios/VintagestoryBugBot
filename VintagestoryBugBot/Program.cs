using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Octokit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace VintagestoryBugBot;

public class VintagestoryBugBot
{
    private readonly DiscordSocketClient _client;
    internal static Config Config;
    private SocketGuild? _guild;
    private readonly Data _data;

    private const string ReportMessageName = "Create Bug Report";
    private const string ReportUpdateName = "Update Bug Report";
    private const string ReportAddCommentName = "Add Report Comment";
    private readonly string _dataFile;
    private bool _createCommand;

    private VintagestoryBugBot(string[] args)
    {
        var basePath = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
        if (Environment.GetEnvironmentVariable("CREATE_COMMANDS") == "true")
        {
            _createCommand = true;
        }

        var configFile = Path.Combine(basePath, "config.json");
        _dataFile = Path.Combine(basePath, "data.json");
        
        if (File.Exists(configFile))
        {
            var jsonConfig = File.ReadAllText(configFile);
            Config = JsonSerializer.Deserialize<Config>(jsonConfig)!;
        }
        else
        {
            Config = Config.FromEnv();
        }

        if (File.Exists(_dataFile))
        {
            var jsonData = File.ReadAllText(_dataFile);
            _data = JsonSerializer.Deserialize<Data>(jsonData)!;
        }
        else
        {
            _data = new Data();
            var jsonData = JsonSerializer.Serialize(_data);
            File.WriteAllText(_dataFile, jsonData);
        }


        Config.Validate();
        

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessageReactions |
                             GatewayIntents.MessageContent |
                             GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true
        };

        _client = new DiscordSocketClient(config);
    }

    public static Task Main(string[] args) => new VintagestoryBugBot(args).MainAsync();

    private async Task MainAsync()
    {
        await GitHubHelper.GetCredentialsForAppAsync();

        _client.Log += Log;
        _client.Ready += ReadyHandler;
        _client.MessageCommandExecuted += MessageCommandExecutedHandler;

        await _client.LoginAsync(TokenType.Bot, Config.DC_TOKEN);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task ReadyHandler()
    {
        Console.WriteLine($"Bot {_client.CurrentUser.Username} ready");
        _guild = _client.GetGuild(Config.DC_GUILD_ID);
        if (_guild is null)
        {
            Console.WriteLine("Cannot register commands since guild was not found: check DC_GuildId in config.json");
            _client.Dispose();
            Environment.Exit(1);
        }

        if (_createCommand)
        {
            // ready handler will be run if the reconnects when losing connection
            _createCommand = false;
            CreateCommands();
        }
        return Task.CompletedTask;
    }

    private async void CreateCommands()
    {
        var bugReportMessage = new MessageCommandBuilder();
        bugReportMessage.WithName(ReportMessageName);

        var bugReportUpdate = new MessageCommandBuilder();
        bugReportUpdate.WithName(ReportUpdateName);

        var bugReportComment = new MessageCommandBuilder();
        bugReportComment.WithName(ReportAddCommentName);
        Console.WriteLine("Commands created");

        try
        {
            // await _guild.DeleteApplicationCommandsAsync();
            await _guild!.BulkOverwriteApplicationCommandAsync(new ApplicationCommandProperties[]
                { bugReportMessage.Build(), bugReportUpdate.Build(), bugReportComment.Build() });
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    private Task MessageCommandExecutedHandler(SocketMessageCommand messageCommand)
    {
        switch (messageCommand.CommandName)
        {
            case ReportMessageName:
            {
                ExecuteBugFromMessage(messageCommand);
                break;
            }
            case ReportUpdateName:
            {
                ExecuteUpdateReport(messageCommand);
                break;
            }
            case ReportAddCommentName:
            {
                ExecuteAddCommentReport(messageCommand);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private void ExecuteAddCommentReport(SocketMessageCommand messageCommand)
    {
        Task.Run(async () =>
        {
            try
            {
                if (messageCommand.Channel is not SocketThreadChannel thread ||
                    thread.ParentChannel.Id != Config.DC_CHANNEL_ID)
                {
                    await messageCommand.RespondAsync($"You can only update bug reports from <#{Config.DC_CHANNEL_ID}>",
                        ephemeral: true);
                    return;
                }

                if (!HasPrivilege((messageCommand.User as SocketGuildUser)!))
                {
                    await messageCommand.RespondAsync("You are not allowed to do that", ephemeral: true);
                    return;
                }

                if (thread.ParentChannel is not SocketForumChannel)
                {
                    await messageCommand.RespondAsync("Failed, parent channel is not a forum channel", ephemeral: true);
                    return;
                }

                if (messageCommand.Data.Message.Id == thread.Id)
                {
                    await messageCommand.RespondAsync(
                        "Failed, you can only add a comment that is not the first message in a post", ephemeral: true);
                    return;
                }

                if (_data.IssueMap.TryGetValue(messageCommand.Channel.Id, out var githubIssueNumber))
                {
                    var issue = await GitHubHelper.GetClient().Issue
                        .Get(Config.GH_OWNER, Config.GH_NAME, githubIssueNumber);
                    await GitHubHelper.GetClient().Issue.Comment
                        .Create(Config.GH_OWNER, Config.GH_NAME, githubIssueNumber,
                            CreateCommentBody(messageCommand, thread));

                    await messageCommand.RespondAsync("Added comment to issue");
                    Console.WriteLine($"Added comment to issue: {thread.Name} | {issue.HtmlUrl}");
                }
                else
                {
                    await messageCommand.RespondAsync("Adding comment failed, could not get issue for this post",
                        ephemeral: true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private void ExecuteUpdateReport(SocketMessageCommand messageCommand)
    {
        Task.Run(async () =>
        {
            try
            {
                if (messageCommand.Channel is not SocketThreadChannel thread ||
                    thread.ParentChannel.Id != Config.DC_CHANNEL_ID)
                {
                    await messageCommand.RespondAsync($"You can only update bug reports from <#{Config.DC_CHANNEL_ID}>",
                        ephemeral: true);
                    return;
                }

                if (!HasPrivilege((messageCommand.User as SocketGuildUser)!))
                {
                    await messageCommand.RespondAsync("You are not allowed to do that", ephemeral: true);
                    return;
                }

                if (thread.ParentChannel is not SocketForumChannel)
                {
                    await messageCommand.RespondAsync("Failed, parent channel is not a forum channel", ephemeral: true);
                    return;
                }

                if (messageCommand.Data.Message.Id != thread.Id)
                {
                    await messageCommand.RespondAsync(
                        "Failed, you can only update an issue form the first message in a post", ephemeral: true);
                    return;
                }

                if (_data.IssueMap.TryGetValue(messageCommand.Channel.Id, out var githubIssueNumber))
                {
                    var issue = await GitHubHelper.GetClient().Issue
                        .Get(Config.GH_OWNER, Config.GH_NAME, githubIssueNumber);
                    var issueUpdate = issue.ToUpdate();
                    issueUpdate.Title = thread.Name;
                    issueUpdate.Body = CreateIssueBody(messageCommand, thread);
                    await GitHubHelper.GetClient().Issue
                        .Update(Config.GH_OWNER, Config.GH_NAME, githubIssueNumber, issueUpdate);
                    await messageCommand.RespondAsync("Issue updated");
                    Console.WriteLine($"Updated Issue: {thread.Name} | {issue.HtmlUrl}");
                }
                else
                {
                    await messageCommand.RespondAsync("Issue update failed, could not get issue for this post",
                        ephemeral: true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }


    private void ExecuteBugFromMessage(SocketMessageCommand messageCommand)
    {
        Task.Run(async () =>
        {
            try
            {
                if (messageCommand.Channel is not SocketThreadChannel thread ||
                    thread.ParentChannel.Id != Config.DC_CHANNEL_ID)
                {
                    await messageCommand.RespondAsync($"You can only creat bug reports from <#{Config.DC_CHANNEL_ID}>",
                        ephemeral: true);
                    return;
                }

                if (!HasPrivilege((messageCommand.User as SocketGuildUser)!))
                {
                    await messageCommand.RespondAsync("You are not allowed to do that", ephemeral: true);
                    return;
                }

                if (thread.ParentChannel is not SocketForumChannel forumChannel)
                {
                    await messageCommand.RespondAsync("Failed, parent channel is not a forum channel", ephemeral: true);
                    return;
                }

                if (messageCommand.Data.Message.Id != thread.Id)
                {
                    await messageCommand.RespondAsync(
                        "Failed, you can only create an issue form the first message in a post", ephemeral: true);
                    return;
                }

                if (_data.IssueMap.ContainsKey(messageCommand.Channel.Id))
                {
                    await messageCommand.RespondAsync("Failed, issue already exists", ephemeral: true);
                    return;
                }

                var newIssue = new NewIssue(thread.Name)
                {
                    Body = CreateIssueBody(messageCommand, thread)
                };
                var issue = await GitHubHelper.GetClient().Issue.Create(Config.GH_OWNER, Config.GH_NAME, newIssue);
                _data.IssueMap.Add(thread.Id, issue.Id);

                var jsonData = JsonSerializer.Serialize(_data);
                await File.WriteAllTextAsync(_dataFile, jsonData);

                Console.WriteLine($"Created Issue: {thread.Name} | {issue.HtmlUrl}");
                await thread.SendMessageAsync($"Created Issue: {thread.Name}\n{issue.HtmlUrl}",
                    flags: MessageFlags.SuppressEmbeds);
                await messageCommand.RespondAsync("Done", ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        });
    }

    private static string CreateIssueBody(SocketMessageCommand messageCommand, SocketThreadChannel thread)
    {
        var body =
            $"Reported by Discord User: {messageCommand.Data.Message.Author.Username} in [Discord Channel](https://discord.com/channels/{thread.Guild.Id}/{thread.Id}/{thread.Id})\n\n";
        body += messageCommand.Data.Message.Content;

        if (messageCommand.Data.Message.Attachments.Count > 0)
        {
            var at = messageCommand.Data.Message.Attachments.Select(a =>
                a.ContentType.Contains("image") ? $"![image]({a.Url})" : a.Url);
            body += $"\n\nAttachments:\n{string.Join("\n", at)}";
        }

        return body;
    }

    private static string CreateCommentBody(SocketMessageCommand messageCommand, SocketThreadChannel thread)
    {
        var body =
            $"Comment from Discord User: {messageCommand.Data.Message.Author.Username} from [Discord Message](https://discord.com/channels/{thread.Guild.Id}/{thread.Id}/{messageCommand.Data.Message.Id})\n\n";
        body += messageCommand.Data.Message.Content;

        if (messageCommand.Data.Message.Attachments.Count > 0)
        {
            var at = messageCommand.Data.Message.Attachments.Select(a =>
                a.ContentType.Contains("image") ? $"![image]({a.Url})" : a.Url);
            body += $"\n\nAttachments:\n{string.Join("\n", at)}";
        }

        return body;
    }


    private bool HasPrivilege(SocketGuildUser user)
    {
        return user.GuildPermissions.Administrator ||
               user.Roles.Select(r => r.Id).ToArray().Intersect(Config.DC_REPORTER_ROLE_IDS).Any();
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}