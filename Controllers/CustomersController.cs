using System.Net;
using System.Web.Mvc;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    public class CustomersController : Controller
    {
        private readonly CustomerRepository _repo = new CustomerRepository();

        // GET: /Customers
        //public ActionResult Index()
        //{
        //    var list = _repo.GetAll();
        //    return View(list);
        //}

        public ActionResult Index(int page = 1)
        {
            int pageSize = 6;  // 一頁 6 筆

            int totalCount;
            var items = _repo.GetPaged(page, pageSize, out totalCount);

            CustomerListViewModel model = new CustomerListViewModel();
            model.Items = items;
            model.PageIndex = page;
            model.PageSize = pageSize;
            model.TotalCount = totalCount;

            return View(model);
        }

        // GET: /Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer c = _repo.GetById(id.Value);
            if (c == null)
                return HttpNotFound();

            return View(c);
        }

        // GET: /Customers/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Customer c)
        {
            if (ModelState.IsValid)
            {
                _repo.Insert(c);
                return RedirectToAction("Index");
            }

            return View(c);
        }

        // GET: /Customers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer c = _repo.GetById(id.Value);
            if (c == null)
                return HttpNotFound();

            return View(c);
        }

        // POST: /Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Customer c)
        {
            if (ModelState.IsValid)
            {
                _repo.Update(c);
                return RedirectToAction("Index");
            }

            return View(c);
        }

        // GET: /Customers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer c = _repo.GetById(id.Value);
            if (c == null)
                return HttpNotFound();

            return View(c);
        }

        // POST: /Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            _repo.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
