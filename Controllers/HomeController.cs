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
using Esercitazione.Migrations;

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

        // PAGINA INIZIALE 
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Welcome()
        {
            return View();
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

        // REGISTRAZIONE NUOVI UTENTI
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register (User user, string password)
        {
            if(ModelState.IsValid)
            {
                if(user.Password != password)
                {
                    ModelState.AddModelError("Password", "Password non valida");
                    return View(user);
                }
                if(_dataContext.Users.Any(u=>u.Email == user.Email))
                {
                    ModelState.AddModelError("", "Questa email è già registrata");
                    return View(user);
                }

                _dataContext.Users.Add(user);
                await _dataContext.SaveChangesAsync();

                // autenticazione =>
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties();
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // CREAZIONE
        [Authorize(Roles ="Admin")]
        public IActionResult Create()
        {
            ViewBag.Ingredients = new SelectList(_dataContext.Ingredients, "Id", "Name");
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product, int[] Ingredients)
        {
            if(ModelState.IsValid)
            {
                if(Ingredients != null && Ingredients.Length > 0)
                {
                product.Ingredients = new List<Ingredient>();
                foreach(var ingredientId in Ingredients)
                    {
                        var ingredient = _dataContext.Ingredients.Find(ingredientId);
                        if(ingredient != null)
                        {
                            product.Ingredients.Add(ingredient);
                        }
                    }

                }
                _dataContext.Products.Add(product);
                _dataContext.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Ingredients = new SelectList(_dataContext.Ingredients, "Id", "Name");
            return View(product);
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

        // RESOCONTO DEGLI ORDINI
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReportOrders()
        {
            var orders = await _dataContext.Orders
                .Include(o => o.Product)
                .Include(o => o.User)
                .Where(o => o.Completed && !o.IsInCart) 
                .ToListAsync();

            var totalPrice = orders.Sum(o => o.Quantity * o.Product.Price);
            ViewBag.TotalPrice = totalPrice;

            var orderTotal = new Dictionary<int, decimal>();
            foreach (var order in orders)
            {
                orderTotal[order.OrderId] = order.Quantity * order.Product.Price;
            }
            ViewBag.OrderTotal = orderTotal;
            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> UpdateOrder(Dictionary<int, bool> completed)
        {
            var ordersId = completed.Keys;
            var update = await _dataContext.Orders
                .Where(o => ordersId.Contains(o.OrderId))
                .ToListAsync();

            foreach(var order in update)
            {
                if(completed.TryGetValue(order.OrderId, out bool isCompleted))
                {
                    order.Completed = isCompleted;
                } 
            }
            await _dataContext.SaveChangesAsync();
            return RedirectToAction("ReportORders");
        }

        // CUSTOMER - AGGIUNGI AL CARRELLO
        [Authorize(Roles = "Customer, Admin")]
        public async Task<IActionResult> Cart()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _dataContext.Orders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId && o.IsInCart)
                .ToListAsync();

            return View(cartItem);
        }
        [Authorize(Roles = "Customer, Admin")]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string customerNotes = "")
        {
            var product = await _dataContext.Products.FindAsync(productId);
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _dataContext.Users.FindAsync(userId);

            var existingOrder = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.ProductId == productId && o.UserId == userId);

            if (existingOrder != null)
            {
                existingOrder.Quantity += quantity;
                existingOrder.CustomerNotes = customerNotes;
            }
            else
            {
                var order = new Order
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UserId = userId,
                    User = user,
                    CustomerNotes = customerNotes
                };
                _dataContext.Orders.Add(order);
            }

            await _dataContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // CUSTOMER - MODIFICA QUANTITA' NEL CARRELLO
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Customer, Admin")]
        public async Task<IActionResult> UpdateQuantity(int orderId, int quantity)
        {
            var order = await _dataContext.Orders.FindAsync(orderId);
            if(order != null && quantity >0)
            {
                order.Quantity = quantity;  
                await _dataContext.SaveChangesAsync();
            }
            return RedirectToAction("Cart");
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

            return RedirectToAction("Cart");
        }

        // CUSTOMER - NOTE DEL CLIENTE
        public async Task<IActionResult> UpdateOrderNotes(int orderId, string notes)
        {
            var order = await _dataContext.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.CustomerNotes = notes;
                await _dataContext.SaveChangesAsync();
                return RedirectToAction("Cart");
            }
            return View("Error");
        }

        // CUSTOMER - CHECKOUT
        public async Task<IActionResult> Checkout()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var cartItems = await _dataContext.Orders
                .Include(o => o.Product)
                .Where(o => o.UserId == userId && o.IsInCart) 
                .ToListAsync();

            if (cartItems.Any())
            {
                foreach (var item in cartItems)
                {
                    item.Completed = true; 
                    item.IsInCart = false;   
                }

                await _dataContext.SaveChangesAsync(); 

                int totalDeliveryTime = cartItems.Sum(item => item.Product.DeliveryTimeInMinutes * item.Quantity);
                string customerName = User.FindFirstValue(ClaimTypes.Name);

                ViewBag.TotalDeliveryTime = totalDeliveryTime;
                ViewBag.CustomerName = customerName;

                return View("Checkout"); 
            }
            else
            {
                return RedirectToAction("Cart"); 
            }
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
