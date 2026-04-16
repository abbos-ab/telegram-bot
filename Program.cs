using CargoBot.BotHandlers;
using CargoBot.Data;
using CargoBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connection = Environment.GetEnvironmentVariable("DATABASE_URL");

        var uri = new Uri(connection);

        var userInfo = uri.UserInfo.Split(':');

        var connString =
            $"Host={uri.Host};" +
            $"Port={uri.Port};" +
            $"Username={userInfo[0]};" +
            $"Password={userInfo[1]};" +
            $"Database={uri.AbsolutePath.Trim('/')};" +
            $"SSL Mode=Require;Trust Server Certificate=true";

        services.AddDbContext<KargoDbContext>(options =>
            options.UseNpgsql(connString));

        var botToken = "8697322298:AAEbwhwypGsk4PKKMk8p4LbahOFeW98arAU";

        if (string.IsNullOrEmpty(botToken))
        {
            throw new Exception("DIQQAT: appsettings.json faylidan Telegram Token topilmadi! Iltimos faylni tekshiring va 'Copy to Output Directory' ni yoqing.");
        }

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

        services.AddScoped<TrackingService>();
        services.AddScoped<ExcelImportService>();

        services.AddHostedService<BotUpdateHandler>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KargoDbContext>();
    dbContext.Database.EnsureCreated();
}

await host.RunAsync();