using Esercitazione.Context;
using Esercitazione.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Esercitazione.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _dataContext;

        public HomeController(DataContext dataContext, ILogger<HomeController> logger)
        {
            _logger = logger;
            _dataContext = dataContext;
        }

        public IActionResult Index()
        {
            return View(_dataContext.Products);
        }

        public IActionResult Create()
        {
            return View(); 
        }
        [HttpPost]
        public IActionResult Create(Product product)
        {
            _dataContext.Products.Add(product);
            _dataContext.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
