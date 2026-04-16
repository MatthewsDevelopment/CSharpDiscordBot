using Fluxer.Net;
using Fluxer.Net.Commands;
using Fluxer.Net.Commands.Attributes;
using Fluxer.Net.Data.Enums;
using Fluxer.Net.Gateway.Data;
using Serilog;
using Serilog.Core;
using System.Reflection;

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
				// RestSerilog = Log.Logger as Logger,
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
			await ReplyAsync("Fluxer Bot:\nhelp\nping\nbotinfo\nsay\nadd\nhello");
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
}