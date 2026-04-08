using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;
using Xceed.Document.NET;
using Xceed.Words.NET;
using static TaxiLink.UI.Admin_areas.Models.AdminViewModels;

namespace TaxiLink.UI.Admin_areas.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        // Змінили репозиторій на твій готовий сервіс
        private readonly IOrderService _orderService;

        public DashboardController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                // Ставимо дату з початку минулого року, щоб точно знайти всі твої тести
                StartDate = new DateTime(DateTime.Now.Year - 1, 1, 1),
                EndDate = DateTime.Now
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(DateTime? startDate, DateTime? endDate)
        {
            // Тепер викликаємо твій сервіс. Він під капотом використовує OrderRepository, 
            // де вже прописано .Include(o => o.OrderStatus), тому статуси не будуть null!
            var ordersList = (await _orderService.GetAllOrdersAsync()).ToList();

            if (startDate.HasValue)
                ordersList = ordersList.Where(o => o.CreatedAt.Date >= startDate.Value.Date).ToList();
            if (endDate.HasValue)
                ordersList = ordersList.Where(o => o.CreatedAt.Date <= endDate.Value.Date).ToList();

            // 1. KPI (Цифри для карток)
            var totalOrders = ordersList.Count;
            var totalRevenue = ordersList.Sum(o => o.TotalPrice);
            var averageCheck = totalOrders > 0 ? Math.Round(ordersList.Average(o => o.TotalPrice), 2) : 0;
            var activeDrivers = ordersList.Where(o => o.DriverId != null).Select(o => o.DriverId).Distinct().Count();

            // 2. Графік прибутку
            var revenueByDate = ordersList
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new {
                    date = g.Key.ToString("dd.MM.yyyy"),
                    amount = g.Sum(o => o.TotalPrice)
                })
                .ToList();

            // 3. Графік статусів
            var ordersByStatus = ordersList
                .GroupBy(o => o.OrderStatus?.Name ?? "Нове")
                .Select(g => new {
                    status = g.Key,
                    count = g.Count()
                })
                .ToList();

            return Json(new
            {
                kpi = new { totalOrders, totalRevenue, averageCheck, activeDrivers },
                revenue = revenueByDate,
                statuses = ordersByStatus
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            var orders = (await _orderService.GetAllOrdersAsync()).ToList();

            if (startDate.HasValue) orders = orders.Where(o => o.CreatedAt.Date >= startDate.Value.Date).ToList();
            if (endDate.HasValue) orders = orders.Where(o => o.CreatedAt.Date <= endDate.Value.Date).ToList();

            orders = orders.OrderByDescending(o => o.CreatedAt).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Статистика");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Дата";
                worksheet.Cell(1, 3).Value = "Клієнт";
                worksheet.Cell(1, 4).Value = "Статус";
                worksheet.Cell(1, 5).Value = "Сума (₴)";

                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;

                int row = 2;
                foreach (var order in orders)
                {
                    worksheet.Cell(row, 1).Value = order.Id;
                    worksheet.Cell(row, 2).Value = order.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    worksheet.Cell(row, 3).Value = order.PassengerName ?? "Без імені";
                    worksheet.Cell(row, 4).Value = order.OrderStatus?.Name ?? "Нове";
                    worksheet.Cell(row, 5).Value = order.TotalPrice;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TaxiLink_Report_{DateTime.Now:dd_MM_yyyy}.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToWord(DateTime? startDate, DateTime? endDate)
        {
            var orders = (await _orderService.GetAllOrdersAsync()).ToList();

            if (startDate.HasValue) orders = orders.Where(o => o.CreatedAt.Date >= startDate.Value.Date).ToList();
            if (endDate.HasValue) orders = orders.Where(o => o.CreatedAt.Date <= endDate.Value.Date).ToList();

            // Підрахунок показників
            int totalOrders = orders.Count;
            decimal totalRevenue = orders.Sum(o => o.TotalPrice);
            decimal avgCheck = totalOrders > 0 ? Math.Round(orders.Average(o => o.TotalPrice), 2) : 0;
            int activeDrivers = orders.Where(o => o.DriverId != null).Select(o => o.DriverId).Distinct().Count();

            // Офіційний діловий шрифт
            string fontName = "Times New Roman";

            using (var stream = new MemoryStream())
            {
                using (var document = DocX.Create(stream))
                {
                    // 1. Шапка документа (права сторона)
                    var header = document.InsertParagraph("ЗАТВЕРДЖЕНО\nАдміністрація сервісу TaxiLink\n" + DateTime.Now.ToString("«dd» MMMM yyyy р."));
                    header.Font(fontName).FontSize(12).Alignment = Alignment.right;
                    header.SpacingAfter(40);

                    // 2. Головний заголовок
                    var title = document.InsertParagraph("ОФІЦІЙНИЙ ЗВІТ");
                    title.Font(fontName).FontSize(16).Bold().Alignment = Alignment.center;

                    var subtitle = document.InsertParagraph("про результати діяльності та фінансові показники");
                    subtitle.Font(fontName).FontSize(14).Alignment = Alignment.center;
                    subtitle.SpacingAfter(20);

                    // 3. Період (Курсив)
                    string startStr = startDate.HasValue ? startDate.Value.ToString("dd.MM.yyyy") : "початку роботи";
                    string endStr = endDate.HasValue ? endDate.Value.ToString("dd.MM.yyyy") : DateTime.Now.ToString("dd.MM.yyyy");
                    var period = document.InsertParagraph($"Звітний період: з {startStr} по {endStr}");
                    period.Font(fontName).FontSize(12).Italic().Alignment = Alignment.left;
                    period.SpacingAfter(20);

                    // 4. Офіційна таблиця з даними
                    var table = document.InsertTable(4, 2);
                    table.Design = TableDesign.LightGrid; // Строгий стиль таблиці
                    table.Alignment = Alignment.center;

                    // Заповнюємо рядки (Текст - зліва, Цифри - справа жирним)
                    table.Rows[0].Cells[0].Paragraphs.First().Append("Загальна кількість виконаних замовлень:").Font(fontName).FontSize(12);
                    table.Rows[0].Cells[1].Paragraphs.First().Append($"{totalOrders} шт.").Font(fontName).FontSize(12).Bold();

                    table.Rows[1].Cells[0].Paragraphs.First().Append("Загальний обіг (дохід) компанії:").Font(fontName).FontSize(12);
                    table.Rows[1].Cells[1].Paragraphs.First().Append($"{totalRevenue} ₴").Font(fontName).FontSize(12).Bold();

                    table.Rows[2].Cells[0].Paragraphs.First().Append("Середній чек по системі:").Font(fontName).FontSize(12);
                    table.Rows[2].Cells[1].Paragraphs.First().Append($"{avgCheck} ₴").Font(fontName).FontSize(12).Bold();

                    table.Rows[3].Cells[0].Paragraphs.First().Append("Кількість унікальних водіїв на лінії:").Font(fontName).FontSize(12);
                    table.Rows[3].Cells[1].Paragraphs.First().Append($"{activeDrivers} осіб").Font(fontName).FontSize(12).Bold();

                    // Робимо таблицю трохи ширшою для краси
                    foreach (var row in table.Rows)
                    {
                        row.Cells[0].Width = 350;
                        row.Cells[1].Width = 150;
                    }

                    document.InsertParagraph("").SpacingAfter(40); // Відступ після таблиці

                    // 5. Місця для підписів
                    var footer1 = document.InsertParagraph("Генеральний директор     _________________   / ____________ /");
                    footer1.Font(fontName).FontSize(12);
                    footer1.SpacingAfter(20);

                    var footer2 = document.InsertParagraph("Головний бухгалтер       _________________   / ____________ /");
                    footer2.Font(fontName).FontSize(12);
                    footer2.SpacingAfter(10);

                    // 6. Імітація печатки
                    var stamp = document.InsertParagraph("М.П.");
                    stamp.Font(fontName).FontSize(12).Bold().Alignment = Alignment.left;

                    document.Save();
                }

                // Файл тепер матиме назву типу TaxiLink_Official_Report_07_04_2026.docx
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"TaxiLink_Official_Report_{DateTime.Now:dd_MM_yyyy}.docx");
            }
        }
    }
}
