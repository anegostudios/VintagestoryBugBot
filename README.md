# VintagestoryBugBot 

The VintagestoryBugBot is a bot that can link and forward bugreports made in a Discord Forum Channel to a GitHub Issue Tracker.


# Features
- Create a GitHub Issue from a Post in a Forum Channel from Discord
- Allows to update the Issue on GitHub from the Post if it gets changed
- Add messages from a Post as comments to the GitHub Issue

# Usage / Workflow

- A user creates a new post in the Discord Forum Channel  where people can discus it.
- If someone with the special roles (`DC_REPORTER_ROLE_IDS`) looks into it and decides it should be on github can then do `Create Bug Report` by right clicking on the initial message of the post.
- If some valuable information is added in the post a person with special role can click on that message and add it to the github issue `Add Report Comment`.
- If due to more investigation the original issue report/description needs to change the original creator of the post can do so by editing the initial post in discord and then a person with special role can sync it to github using `Update Bug Report`

# Permission
The bot can be used by Users with the Administrative Discord permission or with the roles specified in `DC_REPORTER_ROLE_IDS`

# Config

[Create a Github App](https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/registering-a-github-app)

[Create a Discord Bot](https://discord.com/developers/docs/getting-started)

When starting the bot you can specify the path where to look for the config.json and data.json file.

`VintagestoryBugBot /my/custom/path`

If you do not specify a path it will look next to the binary for the config.json and data.json file.

If you do not want to use a config file you can also specify all options as Environment Variables (Docker).
Further on the Github repo you will find a docker-compose.yml to easily deploy it.

With the Environment Variable `CREATE_COMMANDS` you can make the bot Create the commands in the Discord Server/Guild. If you do not do this you wont see the commands (Needs to be run only once).
It will confirm creating commands with `Commands created` on the console.

```jsonc
{
  // Github user name
  "GH_OWNER": "",
  // Github repository name
  "GH_NAME": "",
  // Github App key (private key) - you get that when creating the app
  "GH_APP_KEY": "",
  // Github AppID - Settings > Developer settings > GitHub Apps > YourAppName
  "GH_APP_ID": "",
  // Github App installadtion ID - YourRepo > Settings > Github Apps > YourAppName > Configure > In the URL is your InstallID
  "GH_INSTALL_ID": 0,
  // Discord Guild/Server ID - User Settings > Advanced > Enbale Developer Mode -- Rigth click on your server logo or name > Copy Server ID
  "DC_GUILD_ID": 0,
  // Discord Channel ID - (Developer Mode Enabled see above) Rigth click on the forum channel > Copy Channel ID
  "DC_CHANNEL_ID": 0,
  // Discord Bot/App Token - https://discord.com/developers > Your App > Bot > Reset Token or copy when creating a new Bot
  "DC_TOKEN": "",
  // Discord role ID's that are allowd to use the bot commands
  "DC_REPORTER_ROLE_IDS": [ 0 ]
}
```

