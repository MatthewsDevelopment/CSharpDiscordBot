using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Net.Http;

public class CommandsModule : ModuleBase<SocketCommandContext>
{
	private readonly TagSettings _tagSettings;
	public CommandsModule(TagSettings tagSettings)
	{
		_tagSettings = tagSettings;
	}

	[Command("help")]
	[Summary("Help menu")]
	public async Task HelpAsync()
	{
		var client = Context.Client;

		var embed = new EmbedBuilder();
		embed.Title = $"{client.CurrentUser.Username}";
		embed.Description = "help\nping\nbotinfo\nserverinfo\nsay\nwsay\ndice\ntag\ntaglist\ntagadd\ntagremove\ndataremove";
		embed.Color = new Color(0, 150, 255);
		embed.WithFooter(footer => footer.Text = $"{Context.User.Username} | {DateTimeOffset.UtcNow.ToString("g")}");
		embed.WithCurrentTimestamp();
		await ReplyAsync(embed: embed.Build());
	}

	[Command("ping")]
	[Summary("Get the latency of the bot")]
	public async Task PingAsync()
	{
		await ReplyAsync($"Pong!\nLatency: **{Context.Client.Latency}ms**.");
	}
	
	[Command("botinfo")]
	[Summary("Get information on the bot")]
	public async Task BotInfoAsync()
	{
		var client = Context.Client;
        
		var embed = new EmbedBuilder()
			.WithTitle($"🤖 {client.CurrentUser.Username} Information")
			.WithThumbnailUrl(client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
			.WithColor(Color.Blue)
			.AddField("Discord User ID", client.CurrentUser.Id, true)
			.AddField("Status", client.Status, true)
			.AddField("Library", "Discord.Net", true)
			.AddField("Invite the Bot", $"https://discord.com/api/oauth2/authorize?client_id={client.CurrentUser.Id}&permissions=0&integration_type=0&scope=bot+applications.commands", true)
			.AddField("Source Code", "https://codeberg.org/MatthewsDevelopment/CSharpDiscordBot", true);
		await ReplyAsync(embed: embed.Build());
	}

	[Command("serverinfo")]
	[Summary("Displays information about the current Discord server.")]
	public async Task ServerInfoAsync()
	{
		var guild = Context.Guild;

		var embed = new EmbedBuilder()
			.WithTitle($"🏛️ Server Information")
			.WithThumbnailUrl(guild.IconUrl)
			.WithColor(Color.Blue)
			.AddField("Guild name", guild.Name, true)
			.AddField("Guild ID", guild.Id, true)
			.AddField("Members count", guild.MemberCount, true)
			.AddField("Owner", guild.Owner.Username, true)
			.AddField("Region", guild.VoiceRegionId, true)
			.WithFooter(footer => footer.Text = $"Guild creation date: {guild.CreatedAt.ToString("g")}");
		await ReplyAsync(embed: embed.Build());
	}

	[Command("helloworld")]
	[Summary("Hello World!")]
	public async Task HelloWorldAsync()
	{
		await ReplyAsync("Hello World!.");
	}

	[Command("say")]
	[Summary("Make me say things")]
	public async Task SayAsync([Remainder] string message)
	{
		await ReplyAsync(message);
		await Context.Message.DeleteAsync();
	}

    [RequireUserPermission(GuildPermission.ManageWebhooks)]
    [Command("wsay")]
    [Summary("Make me say things through a webhook")]
    public async Task SendWebhookAsync(string webhookUrl, [Remainder] string message)
    {
        if (!Uri.IsWellFormedUriString(webhookUrl, UriKind.Absolute))
        {
            await ReplyAsync("❌ You did not provide a valid webhook url");
            return;
        }

        try
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                string jsonPayload = $"{{\"content\": \"{message.Replace("\"", "\\\"")}\"}}";
                var content = new System.Net.Http.StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(webhookUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    await ReplyAsync($"Sent the message to the webhook: `{webhookUrl}`");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await ReplyAsync($"🛑 An error has occured. Status code: **{(int)response.StatusCode}**.\nDetails: `{(errorContent.Length > 100 ? errorContent.Substring(0, 100) + "..." : errorContent)}`");
                }
            }
        }
        catch (Exception ex)
        {
            await ReplyAsync($"ERROR: `{ex.Message}`");
		}
    }





	[Command("dice")]
	[Summary("Roll a 6 sided dice")]
	public async Task RollDiceAsync()
	{
		var random = new Random();
		int result = random.Next(1, 7);
		await ReplyAsync($"🎲 You rolled a: **{result}**! 🎲");
	}





	[Command("tag")]
	[Alias("t")]
	[Summary("Displays the content of a specified tag.")]
	public async Task ShowTagContentAsync(string tagName)
	{
		if (Context.Guild == null)
		{
			await ReplyAsync("This is not a server channel.");
			return;
		}

		string key = tagName.ToLowerInvariant();
        var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);

        if (guildTags.TryGetValue(key, out Tag? tag))
		{
			string unescapedContent = tag.Content.Replace("\\n", "\n");
			string safeContent = unescapedContent
				.Replace("@everyone", "@\u200beveryone")
				.Replace("@here", "@\u200bhere")
				.Replace("<@", "<@\u200b");
			await ReplyAsync(safeContent);
		}
		else
		{
			await ReplyAsync($"❌ Tag `{tagName}` not found.");
		}
	}

	[Command("taglist")]
	[Summary("Lists all available tags for the current server.")]
	public async Task TagListAsync()
	{
		if (Context.Guild == null)
		{
			await ReplyAsync("This is not a server channel.");
			return;
		}

		var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);
		if (guildTags.Count == 0)
		{
			await ReplyAsync("No tags has been added yet for this server.");
			return;
		}

		string tagNames = string.Join(", ", guildTags.Keys);

		var embed = new EmbedBuilder()
			.WithTitle($"Tags for {Context.Guild.Name} ({guildTags.Count} tags for this server)")
			.WithDescription(tagNames)
			.WithColor(Color.Purple);
		await ReplyAsync(embed: embed.Build());
	}

	[RequireUserPermission(GuildPermission.ManageGuild)]
	[Command("tagadd")]
	[Summary("Creates a new tag with the specified content.")]
	public async Task TagCreateAsync(string tagName, [Remainder] string content)
	{
		if (Context.Guild == null)
		{
			await ReplyAsync("This is not a server channel.");
			return;
		}
		string key = tagName.ToLowerInvariant();
		var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);

		if (guildTags.ContainsKey(key))
		{
			await ReplyAsync($"❌ Tag `{tagName}` already exists! Use the `tagdelete` command first.");
			return;
		}

		guildTags.Add(key, new Tag { Content = content, OwnerId = Context.User.Id });
		_tagSettings.Save();
		await ReplyAsync($"Tag `{tagName}` added.");
	}

	[RequireUserPermission(GuildPermission.ManageGuild)]
	[Command("tagremove")]
	[Summary("Deletes a specified tag.")]
	public async Task TagDeleteAsync(string tagName)
	{
		if (Context.Guild == null)
		{
			await ReplyAsync("This is not a server channel.");
			return;
		}

		string key = tagName.ToLowerInvariant();
		var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);

		if (!guildTags.ContainsKey(key))
		{
			await ReplyAsync($"❌ tag not found");
			return;
		}

		var tag = guildTags[key];
		guildTags.Remove(key);
		_tagSettings.Save();
		await ReplyAsync($"Tag `{tagName}` removed");
	}

	[RequireUserPermission(GuildPermission.ManageGuild)]
	[Command("dataremove")]
	[Summary("Delete your server data")]
	public async Task TagCleanupAsync()
	{
		if (Context.Guild == null)
		{
			await ReplyAsync("This is not a server channel.");
			return;
		}

		string id = Context.Guild.Id.ToString();

		if (_tagSettings.Remove(id))
		{
			_tagSettings.Save();
			await ReplyAsync($"All data for **{Context.Guild.Name}** has been deleted..");
		}
		else
		{
			await ReplyAsync("No data found for this server.");
		}
	}
}