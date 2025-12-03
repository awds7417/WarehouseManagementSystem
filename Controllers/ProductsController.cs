using System.Net;
using System.Web.Mvc;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductRepository _repo = new ProductRepository();

        // GET: /Products
        //public ActionResult Index()
        //{
        //    var list = _repo.GetAll();
        //    return View(list);
        //}
        public ActionResult Index(string keyword, int page = 1)
        {
            int pageSize = 6; // 一頁 10 筆，你可以之後改成 20、50...

            int totalCount;
            var items = _repo.Search(keyword, page, pageSize, out totalCount);

            ProductListViewModel model = new ProductListViewModel();
            model.Items = items;
            model.Keyword = keyword;
            model.PageIndex = page;
            model.PageSize = pageSize;
            model.TotalCount = totalCount;

            return View(model);
        }

        // GET: /Products/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product p = _repo.GetById(id.Value);
            if (p == null)
            {
                return HttpNotFound();
            }

            return View(p);
        }

        // GET: /Products/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product p)
        {
            if (ModelState.IsValid)
            {
                _repo.Insert(p);
                return RedirectToAction("Index");
            }

            return View(p);
        }

        // GET: /Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product p = _repo.GetById(id.Value);
            if (p == null)
            {
                return HttpNotFound();
            }

            return View(p);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product p)
        {
            if (ModelState.IsValid)
            {
                _repo.Update(p);
                return RedirectToAction("Index");
            }

            return View(p);
        }

        // GET: /Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product p = _repo.GetById(id.Value);
            if (p == null)
            {
                return HttpNotFound();
            }

            return View(p);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            _repo.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
