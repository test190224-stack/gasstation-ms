using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GasStationMS.Services;

namespace GasStationMS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reports;

        public ReportsController(IReportService reports) => _reports = reports;

        public async Task<IActionResult> Dashboard(int? stationId, DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddDays(-30);
            var t = to ?? DateTime.Today.AddDays(1);
            var vm = await _reports.GetDashboardAsync(stationId, f, t);
            ViewBag.From = f;
            ViewBag.To = t;
            ViewBag.StationId = stationId;
            return View(vm);
        }

        public async Task<IActionResult> Sales(DateTime? from, DateTime? to, int? stationId)
        {
            var f = from ?? DateTime.Today.AddDays(-7);
            var t = to ?? DateTime.Today.AddDays(1);
            var vm = await _reports.GetSalesReportAsync(f, t, stationId);
            return View(vm);
        }

        public async Task<IActionResult> ExportSalesExcel(DateTime from, DateTime to, int? stationId)
        {
            var bytes = await _reports.ExportSalesToExcelAsync(from, to, stationId);
            var fileName = $"Sales_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
