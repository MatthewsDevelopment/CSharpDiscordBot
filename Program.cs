using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
	private DiscordSocketClient _client = null!;
	private CommandService _commands = null!;
	private IServiceProvider _services = null!;
	private Configuration _config = null!;

	private TagSettings _tagSettings = null!;
	public static Task Main(string[] args) => new Program().MainAsync();

	public async Task MainAsync()
	{
		_config = Configuration.Load();
		_tagSettings = TagSettings.Load();
		_client = new DiscordSocketClient(new DiscordSocketConfig
		{
			LogLevel = LogSeverity.Info,
			GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
		});

		_commands = new CommandService(new CommandServiceConfig
		{
			CaseSensitiveCommands = false,
			DefaultRunMode = RunMode.Async,
			LogLevel = LogSeverity.Verbose
		});

		_client.Log += Log;
		_client.Ready += OnReady;

		await _client.LoginAsync(TokenType.Bot, _config.DISCORDBOTTOKEN);
		await _client.StartAsync();
		await InitializeCommandsAsync();
		await Task.Delay(Timeout.Infinite);
	}

	private Task Log(LogMessage msg)
	{
		Console.WriteLine(msg.ToString());
		return Task.CompletedTask;
	}

	private Task OnReady()
	{
		Console.WriteLine($"Bot is connected as {_client.CurrentUser.Username}");
		return Task.CompletedTask;
	}

	private async Task InitializeCommandsAsync()
	{
		_services = new ServiceCollection()
			.AddSingleton(_tagSettings)
			.BuildServiceProvider();
		await _commands.AddModulesAsync(
			assembly: System.Reflection.Assembly.GetEntryAssembly(),
			services: _services);
		_client.MessageReceived += HandleCommandAsync;
	}

	private async Task HandleCommandAsync(SocketMessage arg)
	{
		if (arg is not SocketUserMessage message) return;
		if (message.Author.IsBot) return;
		int argPos = 0;

		if (message.HasStringPrefix(_config.DISCORDBOTPREFIX, ref argPos) || 
			message.HasMentionPrefix(_client.CurrentUser, ref argPos))
		{

			var context = new SocketCommandContext(_client, message);
			var result = await _commands.ExecuteAsync(
				context: context, 
				argPos: argPos, 
				services: _services);

			if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
			{
				Console.WriteLine($"Command Error: {result.ErrorReason}");
				await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
			}
		}
	}
}