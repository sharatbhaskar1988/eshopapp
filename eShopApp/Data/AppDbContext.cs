using Microsoft.EntityFrameworkCore;
using eShopApp.Models;

namespace eShopApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed some products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Pro", Description = "High performance laptop", Price = 1299.99m, ImageUrl = "https://via.placeholder.com/300x200?text=Laptop", Stock = 10 },
            new Product { Id = 2, Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 29.99m, ImageUrl = "https://via.placeholder.com/300x200?text=Mouse", Stock = 50 },
            new Product { Id = 3, Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard", Price = 89.99m, ImageUrl = "https://via.placeholder.com/300x200?text=Keyboard", Stock = 25 },
            new Product { Id = 4, Name = "Monitor 4K", Description = "27 inch 4K monitor", Price = 499.99m, ImageUrl = "https://via.placeholder.com/300x200?text=Monitor", Stock = 15 },
            new Product { Id = 5, Name = "USB Hub", Description = "7 port USB hub", Price = 19.99m, ImageUrl = "https://via.placeholder.com/300x200?text=USB+Hub", Stock = 100 },
            new Product { Id = 6, Name = "Webcam HD", Description = "1080p HD webcam", Price = 59.99m, ImageUrl = "https://via.placeholder.com/300x200?text=Webcam", Stock = 30 }
        );
    }
}
