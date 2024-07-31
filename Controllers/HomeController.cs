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
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _dataContext;

        public HomeController(DataContext dataContext, ILogger<HomeController> logger)
        {
            _logger = logger;
            _dataContext = dataContext;
        }

        // LOGIN
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _dataContext.Users
                    .Include(u => u.Roles)
                    .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Roles.FirstOrDefault()?.Name ?? "Customer"),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) 
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties();

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index"); 
                }

                ModelState.AddModelError(string.Empty, "Login fallito");
            }

            return View(model);
        }

        // LOGOUT 
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize(Roles = "Admin, Customer")]
        public IActionResult Index()
        {
            return View(_dataContext.Products);
        }

        // CREAZIONE
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(); 
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            _dataContext.Products.Add(product);
            _dataContext.SaveChanges();
            return RedirectToAction("Index");
        }

        // PAGINA DETTAGLIO
        [Authorize(Roles = "Admin, Customer")]
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
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var product = _dataContext.Products
                .Include(p => p.Ingredients)
                .FirstOrDefault(p => p.Id == id);

            ViewBag.IngredientsList = _dataContext.Ingredients.Select(i => new SelectListItem { Value = $"{i.Id}", Text = i.Name });

            return View(product);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        private void UpdatedProduct(Product product, int[] Ingredients)
        {
            var currentIngredients = product.Ingredients.Select(i => i.Id).ToList();

            var ingredientToRemove = currentIngredients.Except(Ingredients).ToList();
            foreach(var ingredientId in ingredientToRemove)
            {
                var ingredient = _dataContext.Ingredients.Find(ingredientId);
                if(ingredient != null)
                {
                    product.Ingredients.Remove(ingredient);
                }
            }

            var newIngredient = Ingredients.Except(currentIngredients).ToList();
            foreach(var ingredientId in newIngredient)
            {
                var ingredient = _dataContext.Ingredients.Find(ingredientId);
                if(ingredient != null)
                {
                    product.Ingredients.Add(ingredient);
                }
            }
            _dataContext.Update(product);
        }

        // CANCELLAZIONE
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Product model)
        {
            var product = await _dataContext.Products.SingleAsync(p => p.Id == model.Id);
            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // CUSTOMER - AGGIUNGI AL CARRELLO
        [Authorize(Roles = "Customer, Admin")]
        public async Task<IActionResult> Cart()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _dataContext.Orders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return View(cartItem);
        }
        [Authorize(Roles = "Customer, Admin")]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _dataContext.Products.FindAsync(productId);
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _dataContext.Users.FindAsync(userId);

            var existingOrder = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.ProductId == productId && o.UserId == userId);

            if (existingOrder != null)
            {
                existingOrder.Quantity += quantity;  
            }
            else
            {
                var order = new Order
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UserId = userId,
                    User = user
                };
                _dataContext.Orders.Add(order);
            }

            await _dataContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // CUSTOMER - ELIMINA DAL CARRELLO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int id)
        {
            var order = _dataContext.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order != null)
            {
                _dataContext.Orders.Remove(order);
                _dataContext.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // CUSTOMER - CHECKOUT
        public IActionResult Checkout()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var cartItems = _dataContext.Orders
                .Include(o=>o.Product)
                .Where(o=>o.UserId == userId)
                .ToList();

            int totalDeliveryTime = cartItems.Sum(item => item.Product.DeliveryTimeInMinutes * item.Quantity);
            string customerName = User.FindFirstValue(ClaimTypes.Name);

            ViewBag.TotalDeliveryTime = totalDeliveryTime;
            ViewBag.CustomerName = customerName;    

            return View();
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
