using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    public class InventoryTransactionsController : Controller
    {
        private readonly InventoryTransactionRepository _repo = new InventoryTransactionRepository();
        private readonly ProductRepository _productRepo = new ProductRepository();
        private readonly CustomerRepository _customerRepo = new CustomerRepository();

        // GET: /InventoryTransactions
        //public ActionResult Index()
        //{
        //    var list = _repo.GetAll();
        //    return View(list);
        //}

        public ActionResult Index(int page = 1)
        {
            int pageSize = 6; // 一頁 6 筆

            int totalCount;
            var items = _repo.GetPaged(page, pageSize, out totalCount);

            InventoryTransactionListViewModel model = new InventoryTransactionListViewModel();
            model.Items = items;
            model.PageIndex = page;
            model.PageSize = pageSize;
            model.TotalCount = totalCount;

            return View(model);
        }

        // GET: /InventoryTransactions/Create
        public ActionResult Create(int? productId, string txType)
        {
            BindDropDowns();

            InventoryTransaction model = new InventoryTransaction();
            model.TxDate = DateTime.Now;

            // 預設類型：如果網址沒帶，就預設 "IN"
            if (string.IsNullOrEmpty(txType))
                model.TxType = "IN";
            else
                model.TxType = txType;

            // 預設產品：如果網址有帶 productId，就先選好那個產品
            if (productId.HasValue)
                model.ProductId = productId.Value;

            return View(model);
        }

        // POST: /InventoryTransactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InventoryTransaction tx)
        {
            // 下拉如果選 "(無客戶)"，會傳 0，這邊轉成 null
            if (tx.CustomerId.HasValue && tx.CustomerId.Value == 0)
                tx.CustomerId = null;

            if (ModelState.IsValid)
            {
                try
                {
                    _repo.Insert(tx);
                    return RedirectToAction("Index");
                }
                catch (InvalidOperationException ex)
                {
                    // 庫存不足或商業邏輯錯誤，顯示在頁面上
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    // 其它意料之外的錯誤
                    ModelState.AddModelError("", "寫入進出貨紀錄時發生錯誤：" + ex.Message);
                }
            }

            // 失敗就重新綁定下拉選單，回傳原本畫面
            BindDropDowns();
            return View(tx);
        }


        // GET: /InventoryTransactions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var tx = _repo.GetById(id.Value);
            if (tx == null)
                return HttpNotFound();

            return View(tx);
        }

        // POST: /InventoryTransactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            _repo.Delete(id);
            return RedirectToAction("Index");
        }

        // 共用：綁定產品/客戶下拉選單
        private void BindDropDowns()
        {
            var products = _productRepo.GetAll()
                            .Select(p => new
                            {
                                Id = p.Id,
                                Display = p.ProductCode + " - " + p.Name
                            }).ToList();

            ViewBag.ProductId = new SelectList(products, "Id", "Display");

            var customers = _customerRepo.GetAll()
                             .Select(c => new
                             {
                                 Id = c.Id,
                                 Display = c.CustomerCode + " - " + c.Name
                             }).ToList();

            // 客戶可以不選，所以多一個「空白」
            customers.Insert(0, new { Id = 0, Display = "(無客戶/一般調整)" });

            ViewBag.CustomerId = new SelectList(customers, "Id", "Display");
        }
    }
}
