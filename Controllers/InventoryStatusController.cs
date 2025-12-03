using System;
using System.Text;
using System.Web.Mvc;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    public class InventoryStatusController : Controller
    {
        private readonly InventoryReportRepository _repo = new InventoryReportRepository();

        // 原本的 Index
        //public ActionResult Index()
        //{
        //    var list = _repo.GetInventorySummary();
        //    return View(list);
        //}

        public ActionResult Index(int page = 1)
        {
            int pageSize = 6; // 一頁 6 筆

            int totalCount;
            var items = _repo.GetInventorySummaryPaged(page, pageSize, out totalCount);

            InventoryStatusListViewModel model = new InventoryStatusListViewModel();
            model.Items = items;
            model.PageIndex = page;
            model.PageSize = pageSize;
            model.TotalCount = totalCount;

            return View(model);
        }

        // 新增：匯出 CSV
        public FileResult ExportCsv()
        {
            var list = _repo.GetInventorySummary();

            StringBuilder sb = new StringBuilder();

            // 1) 標題列
            sb.AppendLine("ProductCode,ProductName,Unit,TotalIn,TotalOut,CurrentStock");

            // 2) 每一筆庫存資料一行
            foreach (var item in list)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}",
                    EscapeCsv(item.ProductCode),
                    EscapeCsv(item.ProductName),
                    EscapeCsv(item.Unit),
                    item.TotalIn,
                    item.TotalOut,
                    item.CurrentStock));
            }

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            string fileName = "InventoryStatus_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";

            // 第二個參數是 contentType，讓瀏覽器知道這是 CSV 檔
            return File(bytes, "text/csv", fileName);
        }


        // 小工具：CSV 轉義，避免逗號/雙引號/換行搞亂欄位
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool needQuote = value.Contains(",") || value.Contains("\"") ||
                             value.Contains("\r") || value.Contains("\n");

            if (needQuote)
            {
                value = value.Replace("\"", "\"\""); // 內部雙引號變成兩個
                return "\"" + value + "\"";
            }

            return value;
        }
    }
}
