using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Telegram.Bot;
using CargoBot.Data;
using CargoBot.Services;
using CargoBot.BotHandlers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 1. Ma'lumotlar bazasini ulash
        services.AddDbContext<KargoDbContext>(options =>
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));

        // 2. Telegram Bot xizmatini ulash va Tokenni tekshirish (TO'G'RILANDI)
        var botToken = context.Configuration["TelegramBot:Token"];
        if (string.IsNullOrEmpty(botToken))
        {
            throw new Exception("DIQQAT: appsettings.json faylidan Telegram Token topilmadi! Iltimos faylni tekshiring va 'Copy to Output Directory' ni yoqing.");
        }
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

        // 3. Xizmatlarni qo'shish
        services.AddScoped<TrackingService>();
        services.AddScoped<ExcelImportService>();

        // 4. Botni eshituvchi xizmat
        services.AddHostedService<BotUpdateHandler>();
    })
    .Build();

// 5. BAZANI AVTOMATIK YARATISH (TO'G'RILANDI - Jadvallarni o'zi shakllantiradi)
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KargoDbContext>();
    dbContext.Database.EnsureCreated();
}

// 6. Dasturni uzluksiz ishga tushirish
await host.RunAsync();