using CargoBot.BotHandlers;
using CargoBot.Data;
using CargoBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<KargoDbContext>(options =>
            options.UseSqlite("Data Source=cargo.db"));

        var botToken = "8697322298:AAEbwhwypGsk4PKKMk8p4LbahOFeW98arAU";

        if (string.IsNullOrEmpty(botToken))
        {
            throw new Exception("Bot token topilmadi!");
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