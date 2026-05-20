using Azure.Messaging.ServiceBus;
using eShopApp.Data;
using eShopApp.Models;
using Newtonsoft.Json;

namespace eShopApp.Services;

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(CheckoutViewModel checkout, string sessionId);
    Task<Order?> GetOrderAsync(int orderId);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ICartService _cartService;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ICartService cartService,
        IConfiguration config, ILogger<OrderService> logger)
    {
        _db = db;
        _cartService = cartService;
        _config = config;
        _logger = logger;
    }

    public async Task<Order> PlaceOrderAsync(CheckoutViewModel checkout, string sessionId)
    {
        // Create order
        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}",
            OrderDate = DateTime.UtcNow,
            CustomerName = checkout.CustomerName,
            CustomerEmail = checkout.CustomerEmail,
            TotalAmount = checkout.TotalAmount,
            Status = "Processing",
            OrderItems = checkout.CartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                ProductName = c.ProductName,
                Price = c.Price,
                Quantity = c.Quantity
            }).ToList()
        };

        // Save to Azure SQL
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Send message to Service Bus
        await SendOrderToServiceBusAsync(order);

        // Clear cart from Redis
        await _cartService.ClearCartAsync(sessionId);

        return order;
    }

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        return await _db.Orders.FindAsync(orderId);
    }

    private async Task SendOrderToServiceBusAsync(Order order)
    {
        try
        {
            var connectionString = _config["ServiceBus:ConnectionString"];
            var queueName = _config["ServiceBus:QueueName"] ?? "orders";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Service Bus connection string not configured.");
                return;
            }

            await using var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(queueName);

            var message = new ServiceBusMessage(JsonConvert.SerializeObject(new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                OrderDate = order.OrderDate
            }));

            await sender.SendMessageAsync(message);
            _logger.LogInformation("Order {OrderNumber} sent to Service Bus.", order.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order to Service Bus.");
        }
    }
}
