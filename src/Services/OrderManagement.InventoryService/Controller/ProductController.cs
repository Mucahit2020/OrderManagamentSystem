using Microsoft.AspNetCore.Mvc;
using OrderManagement.InventoryService.Interfaces;

namespace OrderManagement.InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm aktif ürünleri getir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetAllActiveProductsAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// ID'ye göre ürün getir
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        return product != null ? Ok(product) : NotFound($"ID'si {id} olan ürün bulunamadı");
    }

    /// <summary>
    /// Birden fazla ID ile ürünleri getir
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> GetProductsBatch([FromBody] List<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds == null || productIds.Count == 0)
            return BadRequest("Ürün ID'leri boş olamaz");

        var products = await _productService.GetProductsByIdsAsync(productIds, cancellationToken);
        return Ok(products);
    }
}
