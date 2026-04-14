using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CargoBot.Data;
using CargoBot.Models;

namespace CargoBot.Services
{
    public class TrackingService
    {
        private readonly KargoDbContext _dbContext;

        public TrackingService(KargoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GetParcelStatusAsync(string trackCode)
        {
            var parcel = await _dbContext.Parcels.AsNoTracking().FirstOrDefaultAsync(p => p.TrackCode == trackCode);

            if (parcel == null) return "❌ Извините, данный трек-код не найден в базе.";

            DateTime etaTajikistan = parcel.ArrivedAtChina.AddDays(25);
            double daysSinceArrival = (DateTime.UtcNow - parcel.ArrivedAtChina).TotalDays;

            string statusText = daysSinceArrival < 2
                ? "📦 Прибыл на склад в Китае (Еще не отправлен)"
                : daysSinceArrival < 25
                    ? "🚛 В пути в Таджикистан"
                    : "🇹🇯 Прибыл в Таджикистан";

            return $@"Трек-код: {parcel.TrackCode}
Вес: {parcel.Weight:0.000} кг
Дата прибытия на склад: {parcel.ArrivedAtChina:dd.MM.yyyy}
Примерная дата прибытия в Таджикистан: ~{etaTajikistan:dd.MM.yyyy}

ℹ️ Статус: {statusText}";
        }

        public async Task<string> AddToMyParcelsAsync(long chatId, string trackCode)
        {
            var parcelExists = await _dbContext.Parcels.AnyAsync(p => p.TrackCode == trackCode);
            if (!parcelExists) return "❌ Этот трек-код еще не добавлен в базу.";

            var user = await _dbContext.Users.FindAsync(chatId);
            if (user == null)
            {
                user = new User { ChatId = chatId, CreatedAt = DateTime.UtcNow };
                _dbContext.Users.Add(user);
            }

            var alreadySaved = await _dbContext.UserParcels.AnyAsync(up => up.ChatId == chatId && up.TrackCode == trackCode);
            if (alreadySaved) return "⚠️ Данная посылка уже имеется в МОИ ПОСЫЛКИ.";

            _dbContext.UserParcels.Add(new UserParcel { ChatId = chatId, TrackCode = trackCode });
            await _dbContext.SaveChangesAsync();

            return "✅ Посылка успешно добавлена!\nДля просмотра нажмите - \"Мои посылки 🚚\"";
        }

        public async Task<string> GetMyParcelsAsync(long chatId)
        {
            var myParcels = await _dbContext.UserParcels
                .Include(up => up.Parcel)
                .Where(up => up.ChatId == chatId)
                .OrderBy(up => up.AddedAt)
                .ToListAsync();

            if (!myParcels.Any()) return "📦 В вашем списке пока нет посылок.";

            var sb = new StringBuilder();
            sb.AppendLine("📦 Мои посылки:\n");

            int count = 1;
            foreach (var item in myParcels)
            {
                var p = item.Parcel;
                DateTime eta = p.ArrivedAtChina.AddDays(25);

                sb.AppendLine($"{count}) Трек код {p.TrackCode}:");
                sb.AppendLine($"  - Вес: {p.Weight:0.000} кг");
                sb.AppendLine($"  - История перемещений:");
                sb.AppendLine($"    1. Прибыл на склад (Дата: {p.ArrivedAtChina:dd.MM.yyyy})");
                sb.AppendLine($"       Примерная дата прибытия {eta:dd.MM.yyyy}\n");
                count++;
            }
            return sb.ToString().TrimEnd();
        }

        public async Task CleanOldDataAsync()
        {
            try
            {
                var thresholdDate = DateTime.UtcNow.AddDays(-60);
                var oldParcels = _dbContext.Parcels.Where(p => p.ArrivedAtChina < thresholdDate);

                if (await oldParcels.AnyAsync())
                {
                    _dbContext.Parcels.RemoveRange(oldParcels);
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"[AUTO-CLEAN] {DateTime.Now}: Удалены данные, прибывшие в Китай более 60 дней назад.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLEAN ERROR] {ex.Message}");
            }
        }
    }
}