# M X E S - Tags System

M X E S has a tags system that server admins can setup for their server and is available on the Discord bot as well as the Fluxer.app bot. The tags system is useful for FAQs and stuff alike.

For Discord, The tags feature can be setup and used via slash commands or through the prefix commands. The tags feature can be used across both slash commands and prefix commands. That means if your server staff chooses to setup the tags system with the slash commands but the users uses the prefix commands to view the content of the tag, those tags from the slash commands will carry over to the prefix commands since both the slash commaands and prefix commands operates through the same "database".

### Setup a Tag

In order to set up some tags for your server, you need the ManageGuild permission to use. Do note that anyone can execute a tag so make sure to not add sensitive information to your tag content that anyone from your server can see.

Below is a list of commands that you can run to manage your server's tags. Both Discord and Fluxer.app have prefix commands and the slash commands are only in the Discord bot.

- To create a tag, you need to use "<prefix>tagadd  <tagname> <tagcontent>" or "/tag add <tagname> <tagcontent>"
- To remove a tag, you need to use "<prefix>tagremove <tagname>" or "/tag remove <tagname>"
- If you want to see what tags your server has, run <prefix>taglist or "/tag list"
- To execute a tag, you use "<prefix>tag <tagname" or "/tag <tagname" and the bot will display the content of the tag that you set for it.
- To remove your data, have a server admin run the <prefix>dataremove command or /tag dataremove