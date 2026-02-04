# M X E S - Tags System

The tags feature can be setup and used via slash commands or through the prefix commands. The tags feature can be used across both slash commands and prefix commands. That means if your server staff chooses to setup the tags system with the slash commands but the users uses the prefix commands to view the content of the tag, those tags from the slash commands will carry over to the prefix commands since both the slash commaands and prefix commands operates through the same "database".

In order to set up tags for your server, you need the ManageGuild permission to use. Do note that anyone can execute a tag so make sure to not add sensitive information to your tag content that anyone from your server can see.

To create a tag, you need to use "<prefix>tagadd  <tagname> <tagcontent>" or "/tag add <tagname> <tagcontent>"

To remove a tag, you need to use "<prefix>tagremove <tagname>" or "/tag remove <tagname>"

If you want to see what tags your server has, run <prefix>taglist or "/tag list"

To execute a tag, you use "<prefix>tag <tagname" or "/tag <tagname" and the bot will display the content of the tag that you set for it.