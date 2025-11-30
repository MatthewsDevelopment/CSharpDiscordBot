using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class Tag
{
	public string Content { get; set; } = null!;
	public ulong OwnerId { get; set; }
}

public class GuildTags : Dictionary<string, Tag>
{
	// Key: Tag Name (string)
	// Value: Tag Object (Tag)
}

public class TagSettings : Dictionary<string, GuildTags>
{
	public static TagSettings Load()
	{
		if (!File.Exists("settings.json"))
		{
			return new TagSettings();
		}
		string json = File.ReadAllText("settings.json");
		return JsonConvert.DeserializeObject<TagSettings>(json)!;
	}

	public void Save()
	{
		string json = JsonConvert.SerializeObject(this, Formatting.Indented);
		File.WriteAllText("settings.json", json);
	}

	public GuildTags GetOrCreateGuildTags(ulong guildId)
	{
		string id = guildId.ToString();
		if (!this.ContainsKey(id))
		{
			this[id] = new GuildTags();
		}
		return this[id];
	}
}