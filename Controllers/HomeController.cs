using Esercitazione.Context;
using Esercitazione.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Esercitazione.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _dataContext;

        public HomeController(DataContext dataContext, ILogger<HomeController> logger)
        {
            _logger = logger;
            _dataContext = dataContext;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = _dataContext.Users
                    .Include(u => u.Roles)
                    .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);  

                if (user != null)
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Roles.FirstOrDefault()?.Name ?? "Customer")  
                };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties();

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Login fallito come te");
            }
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View(_dataContext.Products);
        }

        // CREAZIONE
        public IActionResult Create()
        {
            return View(); 
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            _dataContext.Products.Add(product);
            _dataContext.SaveChanges();
            return RedirectToAction("Index");
        }

        // PAGINA DETTAGLIO
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _dataContext.Products
                .Include(p => p.Ingredients)  
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // MODIFICA PRODOTTO
        public IActionResult Edit(int id)
        {
            var product = _dataContext.Products
                .Include(p => p.Ingredients)
                .FirstOrDefault(p => p.Id == id);

            ViewBag.IngredientsList = _dataContext.Ingredients.Select(i => new SelectListItem { Value = $"{i.Id}", Text = i.Name });

            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product, int[] Ingredients)
        {
            var p = _dataContext.Products
                .Include(pt => pt.Ingredients)
                .Single(p => p.Id == product.Id);
            p.Name = product.Name;
            p.Price = product.Price;
            p.DeliveryTimeInMinutes = product.DeliveryTimeInMinutes;
            p.Photo = product.Photo;

            UpdatedProduct(p, Ingredients);

            _dataContext.SaveChanges();
            return RedirectToAction("Index");
        }

        private void UpdatedProduct(Product product, int[] Ingredients)
        {
            var existingIngredient = product.Ingredients.Select(i => i.Id).ToList();

            var newIngredient = Ingredients.Except(existingIngredient).ToList();
            if (newIngredient.Any())
            {
                var newIngredients = _dataContext.Ingredients
                    .Where(ingredient => newIngredient.Contains(ingredient.Id))
                    .ToList();

                foreach (var ingredient in newIngredients)
                {
                    product.Ingredients.Add(ingredient);                   
                }
            }
            _dataContext.Update(product);
        }

        // CANCELLAZIONE
        public async Task<IActionResult> Delete(Product model)
        {
            var product = await _dataContext.Products.SingleAsync(p => p.Id == model.Id);
            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
