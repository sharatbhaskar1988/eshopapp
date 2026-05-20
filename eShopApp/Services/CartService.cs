using eShopApp.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace eShopApp.Services;

public interface ICartService
{
    Task<List<CartItem>> GetCartAsync(string sessionId);
    Task AddToCartAsync(string sessionId, CartItem item);
    Task RemoveFromCartAsync(string sessionId, int productId);
    Task ClearCartAsync(string sessionId);
    Task UpdateQuantityAsync(string sessionId, int productId, int quantity);
}

public class CartService : ICartService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cartExpiry = TimeSpan.FromHours(24);

    public CartService(IDistributedCache cache)
    {
        _cache = cache;
    }

    private string CartKey(string sessionId) => $"cart:{sessionId}";

    public async Task<List<CartItem>> GetCartAsync(string sessionId)
    {
        var data = await _cache.GetStringAsync(CartKey(sessionId));
        if (string.IsNullOrEmpty(data)) return new List<CartItem>();
        return JsonConvert.DeserializeObject<List<CartItem>>(data) ?? new List<CartItem>();
    }

    public async Task AddToCartAsync(string sessionId, CartItem item)
    {
        var cart = await GetCartAsync(sessionId);
        var existing = cart.FirstOrDefault(x => x.ProductId == item.ProductId);
        if (existing != null)
            existing.Quantity += item.Quantity;
        else
            cart.Add(item);

        await SaveCartAsync(sessionId, cart);
    }

    public async Task RemoveFromCartAsync(string sessionId, int productId)
    {
        var cart = await GetCartAsync(sessionId);
        cart.RemoveAll(x => x.ProductId == productId);
        await SaveCartAsync(sessionId, cart);
    }

    public async Task ClearCartAsync(string sessionId)
    {
        await _cache.RemoveAsync(CartKey(sessionId));
    }

    public async Task UpdateQuantityAsync(string sessionId, int productId, int quantity)
    {
        var cart = await GetCartAsync(sessionId);
        var item = cart.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = quantity;
        }
        await SaveCartAsync(sessionId, cart);
    }

    private async Task SaveCartAsync(string sessionId, List<CartItem> cart)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cartExpiry
        };
        await _cache.SetStringAsync(CartKey(sessionId), JsonConvert.SerializeObject(cart), options);
    }
}
