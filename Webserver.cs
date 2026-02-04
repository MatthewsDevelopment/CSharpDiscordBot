using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Web;
using Discord.WebSocket;
using System.Net.Http;
using Markdig;

public class Webserver
{
	private readonly HttpListener _listener;
	private readonly DiscordSocketClient _client;
	private readonly int _port;
	private static readonly HttpClient _httpClient = new HttpClient();

	public Webserver(int port, DiscordSocketClient client)
	{
		_port = port;
		_client = client;
		_listener = new HttpListener();
		_listener.Prefixes.Add($"http://*:{_port}/");
	}

	public async Task StartAsync()
	{
		_listener.Start();
		Console.WriteLine($"[Webserver] Webserver is Ready. Running at port: {_port}");
		while (_listener.IsListening)
		{
			var context = await _listener.GetContextAsync();
			_ = ProcessRequestAsync(context);
		}
	}

	private async Task ProcessRequestAsync(HttpListenerContext context)
	{
		var request = context.Request;
		var response = context.Response;

		try
		{
			string path = request.Url.LocalPath.ToLower();
			if (request.Url.LocalPath == "/")
			{
				await ServeIndexPage(response);
				return;
			}
			if (request.Url.LocalPath == "/api")
			{
				await ServeApiInfo(response);
				return;
			}
			if (path == "/docs" || path.StartsWith("/docs/"))
			{
				await ServeDocs(path, response);
				return;
			}

			string content = File.Exists("index.html") ? File.ReadAllText("index.html") : "<h1>404</h1>";
			byte[] buffer = Encoding.UTF8.GetBytes(content);
			response.ContentType = "text/html";
			await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Webserver] ERROR: {ex.Message}");
		}
		finally
		{
			response.Close();
		}
	}

	private async Task ServeIndexPage(HttpListenerResponse response)
	{
		if (!File.Exists("index.html"))
		{
			byte[] err = Encoding.UTF8.GetBytes("<h1>info.html missing</h1>");
			await response.OutputStream.WriteAsync(err, 0, err.Length);
			return;
		}
		string html = await File.ReadAllTextAsync("index.html");
		var user = _client.CurrentUser;
		string avatarUrl = user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl();
		html = html.Replace("{{AvatarUrl}}", avatarUrl)
				   .Replace("{{BotName}}", user.Username)
				   .Replace("{{BotId}}", user.Id.ToString())
				   .Replace("{{Status}}", _client.ConnectionState.ToString())
				   .Replace("{{Latency}}", _client.Latency.ToString())
				   .Replace("{{GuildCount}}", _client.Guilds.Count.ToString());
		byte[] buffer = Encoding.UTF8.GetBytes(html);
		response.ContentType = "text/html";
		await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
	}

	private async Task ServeApiInfo(HttpListenerResponse response)
	{
		var targetFramework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
		var discordNetAssembly = typeof(Discord.WebSocket.DiscordSocketClient).Assembly.GetName().Version;
		var interactionAssembly = typeof(Discord.Interactions.InteractionService).Assembly.GetName().Version;
		var user = _client.CurrentUser;

		var apiResponse = new
		{
			discordbot = new
			{
				discordbot_name = user.Username,
				discordbot_id = user.Id.ToString(),
				discordbot_guilds = _client.Guilds.Count.ToString(),
				discordbot_latency = _client.Latency,
				discordbot_status = _client.ConnectionState.ToString()
			},
			system = new
			{
				environment = new
				{
					target_framework = targetFramework,
					discordnet_version = discordNetAssembly?.ToString(),
					discordnet_interactions_version = interactionAssembly?.ToString()
				},
				osplatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
				servertime = DateTime.UtcNow.ToString("o")
			}
		};

		string json = Newtonsoft.Json.JsonConvert.SerializeObject(apiResponse, Newtonsoft.Json.Formatting.Indented);
		byte[] buffer = Encoding.UTF8.GetBytes(json);
		response.ContentType = "application/json";
		response.StatusCode = (int)HttpStatusCode.OK;
		await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
	}

	private async Task ServeDocs(string path, HttpListenerResponse response)
	{
		if (path == "/docs" || path == "/docs/")
		{
			response.Redirect("/docs/intro");
			return;
		}

		string docName = path.Substring(6).Trim('/');
		string filePath = Path.Combine("docs", $"{docName}.md");
		if (!File.Exists(filePath))
		{
			await Serve404(response);
			return;
		}

		StringBuilder sidebarBuilder = new StringBuilder();
		if (Directory.Exists("docs"))
		{
			var files = Directory.GetFiles("docs", "*.md");
			foreach (var file in files)
			{
				string fileName = Path.GetFileNameWithoutExtension(file);
				string activeClass = fileName.ToLower() == docName.ToLower() ? "active" : "";
				sidebarBuilder.AppendLine($"<a class='{activeClass}' href='/docs/{fileName}'>{fileName}</a>");
			}
		}

		string markdown = await File.ReadAllTextAsync(filePath);
		var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
		string htmlContent = Markdown.ToHtml(markdown, pipeline);

		string fullHtml = $@"
		<!DOCTYPE html>
		<html>
		<head>
			<title>{docName} - Docs</title>
			<link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown.min.css'>
			<style>
				body {{ display: flex; margin: 0; font-family: sans-serif; height: 100vh; }}
				.sidebar {{ width: 250px; background: #f6f8fa; border-right: 1px solid #d0d7de; padding: 20px; overflow-y: auto; }}
				.sidebar h3 {{ font-size: 1.2em; margin-bottom: 15px; }}
				.sidebar a {{ display: block; padding: 5px 10px; color: #0969da; text-decoration: none; border-radius: 6px; }}
				.sidebar a:hover {{ background: #ebf0f4; }}
				.sidebar a.active {{ background: #0969da; color: white; font-weight: bold; }}
				.content {{ flex: 1; padding: 45px; overflow-y: auto; }}
			</style>
		</head>
		<body>
			<div class='sidebar'>
				<h3>Documentation</h3>
				{sidebarBuilder}
				<hr>
				<a href='/'>Home Page</a>
			</div>
			<div class='content markdown-body'>
				{htmlContent}
			</div>
		</body>
		</html>";

		byte[] buffer = Encoding.UTF8.GetBytes(fullHtml);
		response.ContentType = "text/html";
		await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
	}

	private async Task Serve404(HttpListenerResponse response)
	{
		response.StatusCode = (int)HttpStatusCode.NotFound;
		byte[] buffer = Encoding.UTF8.GetBytes("<h1>404 - Not Found</h1>");
		await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
	}
}