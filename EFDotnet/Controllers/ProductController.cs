using EFDotnet.Data;
using EFDotnet.Models;
using EFDotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EFDotnet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RedisCacheService _redisCacheService;

    public ProductController(AppDbContext context, RedisCacheService redisCacheService)
    {
        _context = context;
        _redisCacheService = redisCacheService;
    }


    // GET :api/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        const string cacheKey = "all_products";
        var cacheProducts = await _redisCacheService.GetAsync<List<Product>>(cacheKey);

        if (cacheProducts == null)
        {
            cacheProducts = await _context.Products.ToListAsync();
            await _redisCacheService.SetAsync(cacheKey, cacheProducts, TimeSpan.FromMinutes(5));
        }

        return Ok(cacheProducts);
    }

    //GET: api/Products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        string cacheKey = $"product_{id}";
        var cacheProduct = await _redisCacheService.GetAsync<Product>(cacheKey);
        if (cacheProduct == null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            await _redisCacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));
            return Ok(product);
        }

        return cacheProduct;
    }

    // POST: api/Products
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        // Invalidate product list cache to reflect the new addition
        await _redisCacheService.RemoveAsync("all_products");
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // PUT: api/Products/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(int id, Product product)
    {
        if (id != product.Id) return BadRequest();

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Products.Any(e => e.Id == id)) return NotFound();
            else throw;
        }

        // Update cache with the modified product and invalidate product list cache
        await _redisCacheService.SetAsync($"product_{id}", product, TimeSpan.FromMinutes(5));
        await _redisCacheService.RemoveAsync("all_products");

        return NoContent();
    }

    // DELETE: api/Products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        // Remove product from cache and invalidate product list cache
        await _redisCacheService.RemoveAsync($"product_{id}");
        await _redisCacheService.RemoveAsync("all_products");

        return NoContent();
    }
}