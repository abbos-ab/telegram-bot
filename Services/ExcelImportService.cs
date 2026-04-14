using System.Globalization;
using ClosedXML.Excel;
using CargoBot.Data;
using CargoBot.Models;

namespace CargoBot.Services
{
    public class ExcelImportService
    {
        private readonly KargoDbContext _dbContext;

        public ExcelImportService(KargoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> ProcessExcelFileAsync(Stream fileStream)
        {
            int addedCount = 0;
            int updatedCount = 0;

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);
                var headerRow = worksheet.Row(1);

                int inDateCol = -1, outDateCol = -1;
                int inWeightCol = -1, outWeightCol = -1;

                for (int i = 1; i <= 20; i++)
                {
                    string header = headerRow.Cell(i).GetString().Trim();
                    if (header.Contains("入库时间")) inDateCol = i;
                    if (header.Contains("出库时间")) outDateCol = i;
                    if (header.Contains("入库重量")) inWeightCol = i;
                    if (header.Contains("出库重量")) outWeightCol = i;
                }

                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var trackCode = row.Cell(1).GetString().Trim();
                    if (string.IsNullOrEmpty(trackCode)) continue;

                    DateTime arrivedAt = default;
                    double weight = 0;

                    if (outDateCol > 0 && !row.Cell(outDateCol).IsEmpty())
                        arrivedAt = ParseDate(row.Cell(outDateCol));

                    if (arrivedAt == default && inDateCol > 0 && !row.Cell(inDateCol).IsEmpty())
                        arrivedAt = ParseDate(row.Cell(inDateCol));

                    if (arrivedAt == default) arrivedAt = ParseDate(row.Cell(7));

                    if (outWeightCol > 0 && !row.Cell(outWeightCol).IsEmpty())
                        weight = ParseWeight(row.Cell(outWeightCol));

                    if (weight == 0 && inWeightCol > 0 && !row.Cell(inWeightCol).IsEmpty())
                        weight = ParseWeight(row.Cell(inWeightCol));

                    if (weight == 0) weight = ParseWeight(row.Cell(9)); 

                    if (arrivedAt == default) arrivedAt = DateTime.UtcNow;

                    var existingParcel = _dbContext.Parcels.FirstOrDefault(p => p.TrackCode == trackCode);

                    if (existingParcel != null)
                    {
                        existingParcel.ArrivedAtChina = arrivedAt;
                        existingParcel.Weight = weight;
                        existingParcel.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                    else
                    {
                        _dbContext.Parcels.Add(new Parcel
                        {
                            TrackCode = trackCode,
                            ArrivedAtChina = arrivedAt,
                            Weight = weight,
                            UpdatedAt = DateTime.UtcNow
                        });
                        addedCount++;
                    }
                }

                await _dbContext.SaveChangesAsync();
                return $"✅ База успешно обновлена!\nДобавлено: {addedCount} шт.\nОбновлено: {updatedCount} шт.";
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при чтении файла: {ex.Message}";
            }
        }

        private DateTime ParseDate(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
            DateTime.TryParse(cell.GetString().Trim(), out DateTime dt);
            return dt;
        }

        private double ParseWeight(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number) return cell.GetDouble();

            string s = cell.GetString().ToLower()
                .Replace("kg", "")
                .Replace("кг", "")
                .Replace(",", ".")
                .Trim();

            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double w);
            return w;
        }
    }
}