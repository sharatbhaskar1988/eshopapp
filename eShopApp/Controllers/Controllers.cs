using eShopApp.Data;
using eShopApp.Models;
using eShopApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eShopApp.Controllers;

// ── HOME CONTROLLER ──
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _db.Products.ToListAsync();
        return View(products);
    }
}

// ── CART CONTROLLER ──
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly AppDbContext _db;

    public CartController(ICartService cartService, AppDbContext db)
    {
        _cartService = cartService;
        _db = db;
    }

    private string SessionId
    {
        get
        {
            var id = HttpContext.Request.Cookies["cart_id"];
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                HttpContext.Response.Cookies.Append("cart_id", id, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true
                });
            }
            return id;
        }
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(SessionId);
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();

        await _cartService.AddToCartAsync(SessionId, new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Price = product.Price,
            Quantity = quantity
        });

        TempData["Success"] = $"{product.Name} added to cart!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cartService.RemoveFromCartAsync(SessionId, productId);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(SessionId, productId, quantity);
        return RedirectToAction("Index");
    }
}

// ── CHECKOUT CONTROLLER ──
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CheckoutController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    private string SessionId
    {
        get
        {
            var id = HttpContext.Request.Cookies["cart_id"];
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                HttpContext.Response.Cookies.Append("cart_id", id, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true
                });
            }
            return id;
        }
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(SessionId);
        if (!cart.Any()) return RedirectToAction("Index", "Cart");

        var model = new CheckoutViewModel
        {
            CartItems = cart,
            TotalAmount = cart.Sum(x => x.Total)
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var cart = await _cartService.GetCartAsync(SessionId);
        model.CartItems = cart;
        model.TotalAmount = cart.Sum(x => x.Total);

        var order = await _orderService.PlaceOrderAsync(model, SessionId);
        return RedirectToAction("Confirmation", new { orderId = order.Id });
    }

    public async Task<IActionResult> Confirmation(int orderId)
    {
        var order = await _orderService.GetOrderAsync(orderId);
        if (order == null) return NotFound();
        return View(order);
    }
}

// ── HEALTH CONTROLLER ──
public class HealthController : Controller
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("/health/live")]
    public IActionResult Live() => Ok(new { status = "Alive", timestamp = DateTime.UtcNow });

    [HttpGet("/health/ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            await _db.Database.CanConnectAsync();
            return Ok(new { status = "Ready", database = "Connected", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Not Ready", error = ex.Message });
        }
    }
}