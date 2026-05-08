using Fluxer.Net;
using Fluxer.Net.Commands;
using Fluxer.Net.Commands.Attributes;
using Fluxer.Net.Data.Enums;
using Fluxer.Net.Gateway.Data;
using Serilog;
using Serilog.Core;
using System.Reflection;
using System.Text.Json;

public class FluxerTagEntry
{
	public string Content { get; set; }
	public ulong OwnerId { get; set; }
}

public class FluxerTagStorage
{
	public Dictionary<ulong, Dictionary<string, FluxerTagEntry>> GuildData { get; set; } = new();
	private static readonly string FilePath = "settings-fluxer.json";

	public static FluxerTagStorage Load()
	{
		if (!File.Exists(FilePath)) return new FluxerTagStorage();
		try {
			var json = File.ReadAllText(FilePath);
			return JsonSerializer.Deserialize<FluxerTagStorage>(json) ?? new FluxerTagStorage();
		} catch { return new FluxerTagStorage(); }
	}
	public bool ClearGuildData(ulong guildId)
	{
		if (GuildData.ContainsKey(guildId))
		{
			GuildData.Remove(guildId);
			Save();
			return true;
		}
		return false;
	}

	public void Save()
	{
		var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(FilePath, json);
	}

	public Dictionary<string, FluxerTagEntry> GetTagsForGuild(ulong guildId)
	{
		if (!GuildData.ContainsKey(guildId)) GuildData[guildId] = new();
		return GuildData[guildId];
	}
}



public class FluxerBot
{
	private readonly Configuration _config;
	private FluxerClient _client;
	private CommandService _commands;

	public FluxerBot(Configuration config)
	{
		_config = config;
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Console()
			.WriteTo.File($"fluxer-log-{DateTime.Now:yyyy-MM-dd}.log")
			.CreateLogger();
	}

	public async Task StartAsync()
	{
		if (string.IsNullOrEmpty(_config.FLUXERBOTTOKEN))
		{
			Log.Error("FLUXERBOTTOKEN is missing in config.json!");
			return;
		}

		try 
		{
			_client = new FluxerClient(_config.FLUXERBOTTOKEN, new()
			{
				ReconnectAttemptDelay = 2,
				IgnoredGatewayEvents = new() { "PRESENCE_UPDATE" },
				Presence = new PresenceUpdateGatewayData(Status.Online),
				EnableRateLimiting = true
			});
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Fluxer: Something went wrong!");
		}

		_commands = new CommandService(
			logger: Log.Logger as Logger,
			services: null
		);

		await _commands.AddModulesAsync(Assembly.GetExecutingAssembly());

		Log.Information("Fluxer: Registered {ModuleCount} modules with {CommandCount} commands",
			_commands.Modules.Count, _commands.Commands.Count());
		_client.Gateway.MessageCreate += async (data) => 
		{
			await HandleMessageAsync(data);
		};
		await _client.Gateway.ConnectAsync();

		Log.Information("[Fluxer] Bot is Ready. Prefix: {Prefix}", _config.FLUXERBOTPREFIX);
	}

	private async Task HandleMessageAsync(MessageGatewayData messageData)
	{
		if (messageData.Author == null || messageData.Author.IsBot) return;
		string prefix = _config.FLUXERBOTPREFIX;
		if (messageData.Content?.StartsWith(prefix) == true)
		{
			var context = new CommandContext(_client, messageData);
			var result = await _commands.ExecuteAsync(context, prefix.Length);
            
			if (!result.IsSuccess && result.ErrorType != CommandError.UnknownCommand)
			{
				Log.Warning("Something went wrong: {Error}", result.Error);
			}
		}
	}

	public class BasicCommands : ModuleBase
	{
		[Command("help")]
		public async Task HelpCommand()
		{
			await ReplyAsync("Fluxer Bot:\nhelp\nping\nbotinfo\nsay\ndice\nadd\nhello\ntag\ntaglist\ntagadd\ntagremove\ndataremove");
		}

		[Command("ping")]
		public async Task PingCommand()
		{
			await ReplyAsync("Pong!");
		}

		[Command("botinfo")]
		public async Task BotInfoCommand()
		{
			await ReplyAsync($"Fluxer Bot Info:\nUptime: {DateTime.UtcNow:HH:mm:ss}");
		}

		[Command("say")]
		public async Task EchoCommand([Remainder] string message)
		{
			await ReplyAsync(message);
		}
		
		[Command("dice")]
		public async Task RollCommand(int sides = 6)
		{
			var result = Random.Shared.Next(1, sides + 1);
			await ReplyAsync($"You rolled a **{result}**!");
		}

		[Command("hello")]
		[Alias("hi")]
		public async Task HelloCommand()
		{
			await ReplyAsync($"Hello, <@{Context.User.Id}>! 👋");
		}

		[Command("add")]
		public async Task AddCommand(int a, int b)
		{
			await ReplyAsync($"{a} + {b} = {a + b}");
		}
	}
	public class TagModule : ModuleBase
	{
		private static readonly FluxerTagStorage _storage = FluxerTagStorage.Load();
		
		[Command("tag")]
		public async Task ShowTagAsync(string name)
		{
			var guildId = Context.Message.GuildId ?? 0;
			if (guildId == 0) return;

			var tags = _storage.GetTagsForGuild(guildId);
			if (tags.TryGetValue(name.ToLowerInvariant(), out var tag))
			{
				string formattedContent = tag.Content
					.Replace("\\n", "\n") 
					.Replace("@everyone", "@\u200beveryone")
					.Replace("@here", "@\u200bhere");

				await ReplyAsync(formattedContent);
			}
			else 
			{
				await ReplyAsync($"Tag `{name}` not found.");
			}
		}

		[Command("taglist")]
		public async Task ListTagsAsync()
		{
			var guildId = Context.Message.GuildId ?? 0;
			if (guildId == 0)
			{
				await ReplyAsync("This is not a server channel.");
				return;
			}
			var tags = _storage.GetTagsForGuild(guildId);
			if (tags.Count == 0) await ReplyAsync("No tags has been added yet for this server.");
			else await ReplyAsync($"**Tags for this server:**\n{string.Join(", ", tags.Keys)}");
		}

		[Command("tagadd")]
		[RequireUserPermission(Permissions.ManageGuild)]
		public async Task AddTagAsync(string name, [Remainder] string content)
		{
			var guildId = Context.Message.GuildId ?? 0;
			if (guildId == 0)
			{
				await ReplyAsync("This is not a server channel.");
				return;
			}
			var tags = _storage.GetTagsForGuild(guildId);
			string key = name.ToLowerInvariant();

			tags[key] = new FluxerTagEntry { Content = content, OwnerId = Context.User.Id };
			_storage.Save();
			await ReplyAsync($"Tag `{name}` added.");
		}

		[Command("tagremove")]
		public async Task RemoveTagAsync(string name)
		{
			var guildId = Context.Message.GuildId ?? 0;
			if (guildId == 0)
			{
				await ReplyAsync("This is not a server channel.");
				return;
			}
			var tags = _storage.GetTagsForGuild(guildId);

			if (tags.Remove(name.ToLowerInvariant()))
			{
				_storage.Save();
				await ReplyAsync($"Tag `{name}` removed.");
			}
		}
		
		[Command("dataremove")]
		[RequireUserPermission(Permissions.ManageGuild)]
		public async Task DataRemoveAsync()
		{
			var guildId = Context.Message.GuildId ?? 0;
			if (guildId == 0)
			{
				await ReplyAsync("This is not a server channel.");
				return;
			}

			if (_storage.ClearGuildData(guildId))
			{
				await ReplyAsync($"All data for `{guildId}` has been deleted.");
			}
			else
			{
				await ReplyAsync("No data found for this server.");
			}
		}
	}
}