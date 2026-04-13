using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using CargoBot.Services;

namespace CargoBot.BotHandlers
{
    public class BotUpdateHandler : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public BotUpdateHandler(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _botClient = botClient;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: null,
                cancellationToken: stoppingToken
            );

            // 🕒 АВТО-ОЧИСТКА: Удаление данных старше 60 дней по дате ArrivedAtChina
            _ = Task.Run(async () => {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var trackingService = scope.ServiceProvider.GetRequiredService<TrackingService>();
                        await trackingService.CleanOldDataAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AUTO-CLEAN ERROR]: {ex.Message}");
                    }
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
            }, stoppingToken);

            Console.WriteLine("Bot ishga tushdi...");
            await Task.Delay(-1, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            // 🛡️ GLOBAL PROTECTION: Бот никогда не упадет
            try
            {
                if (update.Type != UpdateType.Message || update.Message == null) return;

                var chatId = update.Message.Chat.Id;

                // ADMINLARNI TEKSHIRISH MANTIQI
                var adminSetting = _configuration["TelegramBot:AdminId"] ?? "";
                var adminIds = adminSetting.Split(',').Select(id => id.Trim()).ToList();
                bool isAdmin = adminIds.Contains(chatId.ToString());

                using var scope = _scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<TrackingService>();
                var excelService = scope.ServiceProvider.GetRequiredService<ExcelImportService>();

                // ==========================================
                // 📝 ADMIN EXCEL FAYL TASHASA QABUL QILISH
                // ==========================================
                if (update.Message.Type == MessageType.Document)
                {
                    var document = update.Message.Document;

                    if (document.FileName != null && document.FileName.EndsWith(".xlsx"))
                    {
                        if (!isAdmin)
                        {
                            await botClient.SendMessage(chatId, "⛔ У вас нет прав для обновления базы!", cancellationToken: ct);
                            return;
                        }

                        await botClient.SendMessage(chatId, "⏳ Excel файл принят, идет запись в базу...", cancellationToken: ct);

                        var fileInfo = await botClient.GetFile(document.FileId, ct);
                        using var memoryStream = new MemoryStream();
                        await botClient.DownloadFile(fileInfo.FilePath, memoryStream, ct);
                        memoryStream.Position = 0;

                        string excelResult = await excelService.ProcessExcelFileAsync(memoryStream);
                        await botClient.SendMessage(chatId, excelResult, cancellationToken: ct);
                        return;
                    }
                }

                var text = update.Message.Text?.Trim();

                // ⚠️ ЗАЩИТА ОТ НЕВЕРНЫХ ФАЙЛОВ / INVALID EXPERIENCE
                if (string.IsNullOrEmpty(text))
                {
                    await botClient.SendMessage(chatId, "⚠️ Извините, я понимаю только текстовые команды или трек-коды.", cancellationToken: ct);
                    return;
                }

                var replyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "🔍 Поиск по трек-номеру", "🚚 Мои посылки" },
                    new KeyboardButton[] { "💰 Актуальные цены", "🇨🇳 Адрес склада в Китае" },
                    new KeyboardButton[] { "📍 Наши контакты", "👤 Связь с менеджером" }
                })
                { ResizeKeyboard = true, IsPersistent = true };

                switch (text)
                {
                    case "/start":
                        string welcomeText = "Здравствуйте! PrimeJust&Kargo приветствует вас! 📦\nМы поможем вам быстро и надежно доставить товары из Китая в Таджикистан.\nВ этом боте вы можете самостоятельно отследить свой товар, узнать актуальные цены и получить адрес нашего склада в Китае.\n\nПожалуйста, выберите нужный раздел в меню ниже 👇";
                        await botClient.SendMessage(chatId, welcomeText, replyMarkup: replyKeyboard, cancellationToken: ct);
                        break;

                    case "🔍 Поиск по трек-номеру":
                        await botClient.SendMessage(chatId, "Отправьте трек-код для проверки\nНапример: 78594032289276", cancellationToken: ct);
                        break;

                    case "🚚 Мои посылки":
                        string myParcels = await trackingService.GetMyParcelsAsync(chatId);
                        await botClient.SendMessage(chatId, myParcels, cancellationToken: ct);
                        break;

                    case "💰 Актуальные цены":
                        string prices = @"📦 ТАРИФЫ И ДОСТАВКА ПО НАПРАВЛЕНИЯМ

📍 Город АШТ
Цена за кг: 30 сомони
Цена за куб (м³): 250$

📍 Город ИСФАРА
Цена за кг: 29 сомони
Цена за куб (м³): 250$

📍 Город ХУДЖАНД
Цена за кг: 2.4$
Цена за куб (м³): 250$

🚛 Дополнительные направления:
Также осуществляем доставку до маршруток в города: Мастчох, Истаравшан, Пенджикент.";
                        await botClient.SendMessage(chatId, prices, cancellationToken: ct);
                        break;

                    case "🇨🇳 Адрес склада в Китае":
                        string address = @"📢 Вниманию клиентов! При оформлении заказа указывайте правильный код города:

📍 Для ХУДЖАНДА:
电话: MB-001   15669605500
收货地址：浙江金华义乌市福田街道陶界岭小区22幢5单元一楼11号门  MB-001(имя и номер тел)

📍 Для АШТА:
电话: MB-001   15669605500
收货地址：浙江金华义乌市福田街道陶界岭小区22幢5单元一楼11号门  MB-001АШТ(имя и номер тел)

📍 Для ИСФАРЫ:
电话: MB-001   15669605500
收货地址：浙江金华义乌市福田街道陶界岭小区22幢5单元一楼11号门  MB-001ИСФАРА(имя и номер тел)

⚠️ ЗАПРЕЩЕННЫЕ ТОВАРЫ: Лекарства, холодное оружие, электронные сигареты, 18+, телефоны, ноутбуки, повербанки.";
                        await botClient.SendMessage(chatId, address, cancellationToken: ct);
                        break;

                    case "📍 Наши контакты":
                        string contacts = @"📍 Наши контакты:
Худжанд, 18-й микрорайон, дом 9

💬 Группа для общения: https://t.me/+PRjlvwpDDb1kYjMy
📞 Телефон для связи: 927584638
🕒 Время работы: 8:00 - 20:00";
                        await botClient.SendMessage(chatId, contacts, cancellationToken: ct);
                        break;

                    case "👤 Связь с менеджером":
                        await botClient.SendMessage(chatId, "👨‍💻 Для связи с менеджером напишите сюда: @Vohidova_Mavzuna", cancellationToken: ct);
                        break;

                    default:
                        string result = await trackingService.GetParcelStatusAsync(text);
                        await botClient.SendMessage(chatId, result, cancellationToken: ct);
                        if (!result.Contains("❌"))
                        {
                            string addResult = await trackingService.AddToMyParcelsAsync(chatId, text);
                            await botClient.SendMessage(chatId, addResult, cancellationToken: ct);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // Если произойдет ошибка, бот не упадет, а просто выведет ошибку в консоль
                Console.WriteLine($"[CRITICAL ERROR] {DateTime.Now}: {ex.Message}");
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Xatolik yuz berdi: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}