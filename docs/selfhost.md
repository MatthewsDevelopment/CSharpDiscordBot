# M X E S - Self-hosting

M X E S is open sourced and is licensed under the MIT license. You can obtain the source code from either [Github](https://github.com/MatthewsDevelopment/CSharpDiscordBot) or [Codeberg](https://codeberg.org/MatthewsDevelopment/CSharpDiscordBot)

This project was made for Dotnet 8.0. This project may work on newer Dotnet versions but I am not able to test this. If you use Pterodactyl, try to set the Docket image to Dotnet 8 if possible.

These commands may help for selfhosting this project on a Linux VPS:

- dotnet add package Discord.Net  --version 3.14.0
- dotnet add package Discord.Net.Commands  --version 3.14.0
- dotnet add package Discord.Net.Interactions  --version 3.14.0
- dotnet add package Newtonsoft.Json  --version 13.0.3
- dotnet add package Microsoft.Extensions.DependencyInjection --version 7.0.0
- dotnet add package System.Net.Http --version 4.3.4
- dotnet add package Markdig --version 0.44.0
- dotnet restore
- dotnet build
- dotnet --info
- dotnet build -c Release

Due to my limited resources, I (Matthew's Development) can not offer accurate support for hosting the project on a VPS. I can accurately help with Pterodactyl/Jexactyl users though. This means if you are a VPS user and did not set up Pterodactyl/Jexactyl on your server, you are on your own.