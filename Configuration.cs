using System.IO;
using Newtonsoft.Json;

public class Configuration
{
	public string DISCORDBOTTOKEN { get; set; } = null!; 
	public string DISCORDBOTPREFIX { get; set; } = null!;
	public bool ENABLEWEBSERVER { get; set; } = false;
	public int WEBPORT { get; set; } = 8080;

	public static Configuration Load()
	{
		string json = File.ReadAllText("config.json");
		return JsonConvert.DeserializeObject<Configuration>(json)!; 
	}
}