using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Net.Http;

[Group("tag", "Tag management commands.")]
public class TagSlashGroup : InteractionModuleBase<SocketInteractionContext>
{
	private readonly TagSettings _tagSettings;
	public TagSlashGroup(TagSettings tagSettings)
	{
		_tagSettings = tagSettings;
	}

	[SlashCommand("show", "Displays the content of a specified tag.")]
	public async Task ShowTagContentAsync(
		[Summary("name", "The name of the tag to display.")] 
		string tagName)
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
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
			await RespondAsync(safeContent);
		}
		else
		{
			await RespondAsync($"❌ Tag `{tagName}` not found.", ephemeral: true);
		}
	}

	[SlashCommand("list", "Lists all available tags for the current server.")]
	public async Task TagListAsync()
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}

		var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);
		if (guildTags.Count == 0)
		{
			await RespondAsync("No tags have been added yet for this server.", ephemeral: true);
			return;
		}

		string tagNames = string.Join(", ", guildTags.Keys);

		var embed = new EmbedBuilder()
			.WithTitle($"Tags for {Context.Guild.Name} ({guildTags.Count} tags for this server)")
			.WithDescription(tagNames)
			.WithColor(Color.Purple);
            
		await RespondAsync(embed: embed.Build());
	}

	[SlashCommand("add", "Creates a new tag with the specified content.")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task TagCreateAsync(
		[Summary("name", "The name of the tag")] 
		string tagName,
		[Summary("content", "The content of the tag.")] 
		string content)
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}
        
		string key = tagName.ToLowerInvariant();
		var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);
		if (guildTags.ContainsKey(key))
		{
			await RespondAsync($"❌ Tag `{tagName}` already exists! Use `/tag remove` first.", ephemeral: true);
			return;
		}
		guildTags.Add(key, new Tag { Content = content, OwnerId = Context.User.Id });
		_tagSettings.Save();
		await RespondAsync($"✅ Tag `{tagName}` added.");
	}

	[SlashCommand("remove", "Deletes a specified tag.")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task TagDeleteAsync(
		[Summary("name", "Tag name to delete.")] 
		string tagName)
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}

		string key = tagName.ToLowerInvariant();
		var guildTags = _tagSettings.GetOrCreateGuildTags(Context.Guild.Id);
		if (!guildTags.ContainsKey(key))
		{
			await RespondAsync($"❌ Tag not found.", ephemeral: true);
			return;
		}
		guildTags.Remove(key);
		_tagSettings.Save();
		await RespondAsync($"✅ Tag `{tagName}` removed.");
	}

	[SlashCommand("dataremove", "Delete your server data")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task TagCleanupAsync()
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}

		string id = Context.Guild.Id.ToString();
		if (_tagSettings.Remove(id))
		{
			_tagSettings.Save();
			await RespondAsync($"⚠️ All tag data for **{Context.Guild.Name}** has been deleted.", ephemeral: true);
		}
		else
		{
			await RespondAsync("No tag data found for this server.", ephemeral: true);
		}
	}
}

[Group("info", "Information commands.")]
public class InfoSlashGroup : InteractionModuleBase<SocketInteractionContext>
{
	[SlashCommand("bot", "Get information on the bot.")]
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
		await RespondAsync(embed: embed.Build());
    }

	[SlashCommand("server", "Displays information about the current Discord server.")]
	public async Task ServerInfoAsync()
	{
		var guild = Context.Guild;

		var embed = new EmbedBuilder()
			.WithTitle($"🏛️ Server Information")
			.WithThumbnailUrl(guild.IconUrl)
			.WithColor(Color.Blue)
			.AddField("Guild name", guild.Name, true)
			.AddField("Guild ID", guild.Id, true)
			.AddField("Member Count", guild.MemberCount, true)
			.AddField("Owner", guild.Owner.Username, true)
			.AddField("Region", guild.VoiceRegionId, true)
			.WithFooter(footer => footer.Text = $"Guild creation date: {guild.CreatedAt.ToString("g")}");
		await RespondAsync(embed: embed.Build());
	}

	[SlashCommand("vc", "Get information on a voice channel")]
	public async Task VcInfoAsync([ChannelTypes(ChannelType.Voice, ChannelType.Stage)] IChannel channel)
	{
		var embed = new EmbedBuilder()
			.WithTitle($"Voice Channel Information")
			.WithColor(Color.Blue)
			.AddField("Bitrate", $"{(channel as IVoiceChannel).Bitrate}", true);
		await RespondAsync(embed: embed.Build());
    }
}

[Group("mod", "Moderation commands.")]
public class ModSlashGroup : InteractionModuleBase<SocketInteractionContext>
{
	[SlashCommand("timeout", "Time out a user")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task TimeOutAsync(SocketGuildUser target, int seconds)
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}
		await target.ModifyAsync(u => u.TimedOutUntil = DateTimeOffset.UtcNow.AddSeconds(seconds));
        await RespondAsync($"{target.Mention} has been timed out for {seconds} seconds.");
	}

	[SlashCommand("ban", "Ban a user")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task BanUserAsync(SocketGuildUser target, string reason = "No reason provided")
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}
		await target.BanAsync(0, reason);
        await RespondAsync($"{target.Mention} has been banned for: {reason}");
	}

	[SlashCommand("kick", "Kick a user")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task KickUserAsync(SocketGuildUser target, string reason = "No reason provided")
	{
		if (Context.Guild == null)
		{
			await RespondAsync("This is not a server channel.", ephemeral: true);
			return;
		}

		await target.KickAsync(reason);
        await RespondAsync($"{target.Mention} has been kicked for: {reason}");
	}
}


public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
{
	private static readonly HttpClient _httpClient = new HttpClient();

	[SlashCommand("ping", "Get the latency of the bot.")]
	public async Task PingAsync()
	{
		await RespondAsync($"Pong!\nLatency: **{Context.Client.Latency}ms**.", ephemeral: true);
	}

	[SlashCommand("say", "Make me say things.")]
	public async Task SaySlashAsync(
		[Summary("message", "The message content.")] 
		string message)
	{
		string safeText = message
			.Replace("@everyone", "@\u200beveryone")
			.Replace("@here", "@\u200bhere")
			.Replace("<@", "<@\u200b");
		await RespondAsync(safeText);
	}

	[SlashCommand("wsay", "Make me say things through a webhook.")]
	[RequireUserPermission(GuildPermission.ManageWebhooks)]
	public async Task SendWebhookAsync(
		[Summary("url", "The Discord Webhook URL to send the message to.")] 
		string webhookUrl,
		[Summary("message", "The message content.")] 
		string message)
	{
		if (!Uri.IsWellFormedUriString(webhookUrl, UriKind.Absolute))
		{
			await RespondAsync("❌ You did not provide a valid webhook url", ephemeral: true);
			return;
		}

		try
		{
			string jsonPayload = $"{{\"content\": \"{message.Replace("\"", "\\\"")}\"}}";
			var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(webhookUrl, content);

			if (response.IsSuccessStatusCode)
			{
				await RespondAsync($"Sent the message to the webhook: `{webhookUrl}`", ephemeral: true);
			}
			else
			{
				string errorContent = await response.Content.ReadAsStringAsync();
				await RespondAsync($"🛑 An error has occured. Status code: **{(int)response.StatusCode}**.\nDetails: `{(errorContent.Length > 100 ? errorContent.Substring(0, 100) + "..." : errorContent)}`", ephemeral: true);
			}
		}
		catch (Exception ex)
		{
			await RespondAsync($"❌ An error has occurred while trying to send the webhook: `{ex.Message}`", ephemeral: true);
		}
	}

	[SlashCommand("dice", "Roll a 6 sided dice.")]
	public async Task RollDiceAsync()
	{
		var random = new Random();
		int result = random.Next(1, 7);
		await RespondAsync($"🎲 You rolled a: **{result}**! 🎲");
	}
}