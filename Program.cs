using Discord;
using Discord.Commands;
using Discord.Interactions;
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
	private InteractionService _interactionService = null!;

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
			DefaultRunMode = Discord.Commands.RunMode.Async,
			LogLevel = LogSeverity.Verbose
		});

		_client.Log += Log;
		_client.Ready += ClientReady;
		
		if (_config.ENABLEWEBSERVER)
		{
			_webserver = new Webserver(_config.WEBPORT, _client);
			_ = Task.Run(() => _webserver.StartAsync());
		}
		else
		{
			Console.WriteLine("[Webserver] Webserver is disabled in config.");
		}

		await _client.LoginAsync(TokenType.Bot, _config.DISCORDBOTTOKEN);
		await _client.StartAsync();
		await InitializeServicesAsync();
		await Task.Delay(Timeout.Infinite);
	}

	private Task Log(LogMessage msg)
	{
		Console.WriteLine(msg.ToString());
		return Task.CompletedTask;
	}

	private async Task ClientReady()
	{
		Console.WriteLine($"{_client.CurrentUser.Username} [Discord] Bot is Ready");
		await _interactionService.RegisterCommandsGloballyAsync(true);
		Console.WriteLine($"{_interactionService.SlashCommands.Count} Slash Commands successfully Synced");
	}
	
	private async Task InitializeServicesAsync()
	{
		var commandService = _commands; 
		_services = new ServiceCollection()
			.AddSingleton(_config)
			.AddSingleton(_tagSettings)
			.AddSingleton(_client)
			.AddSingleton(commandService) 
			.AddSingleton<InteractionService>()
			.BuildServiceProvider();

		var tagSettingsInstance = _services.GetRequiredService<TagSettings>();
		var commandsModuleInstance = new CommandsModule(tagSettingsInstance);

		await commandService.AddModuleAsync(
			commandsModuleInstance.GetType(), 
			_services);
            
		_interactionService = _services.GetRequiredService<InteractionService>();
		_interactionService.Log += Log;
		await _interactionService.AddModulesAsync(
			assembly: typeof(Program).Assembly,
			services: _services);

		_client.MessageReceived += HandleCommandAsync;
		_client.InteractionCreated += HandleInteraction;
	}

	private async Task HandleCommandAsync(SocketMessage arg)
	{
		if (arg is not SocketUserMessage message) return;
		if (message.Author.IsBot) return;
		int argPos = 0;
		string prefix = _config.DISCORDBOTPREFIX;

		if (message.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			argPos = prefix.Length;
		}
		else if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
		{
			// argPos is set by HasMentionPrefix
		}
		else
		{
			return;
		}

		var context = new SocketCommandContext(_client, message);
		var result = await _commands.ExecuteAsync(
			context: context, 
			argPos: argPos, 
			services: _services);

		if (!result.IsSuccess) 
		{
			if (result.Error != CommandError.UnknownCommand)
			{
				Console.WriteLine($"Command Error: {result.ErrorReason}");
				await context.Channel.SendMessageAsync($"ERROR: {result.ErrorReason}");
			}
		}
	}
	
	private async Task HandleInteraction(SocketInteraction interaction)
	{
		try
		{
			var context = new SocketInteractionContext(_client, interaction);
			var result = await _interactionService.ExecuteCommandAsync(context, _services);

			if (!result.IsSuccess)
			{
				Console.WriteLine($"Interaction Error: {result.ErrorReason}");
				if (interaction.Type != InteractionType.ApplicationCommand) return;
				if (!interaction.HasResponded)
				{
					await interaction.RespondAsync($"ERROR: {result.ErrorReason}", ephemeral: true);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Interaction Exception: {ex.Message}");
		}
	}

	private Webserver? _webserver;
}