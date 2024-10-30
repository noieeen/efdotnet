using EFDotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace EFDotnet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2); // Specifies a precision of 18 and a scale of 2

        base.OnModelCreating(modelBuilder);
    }
}