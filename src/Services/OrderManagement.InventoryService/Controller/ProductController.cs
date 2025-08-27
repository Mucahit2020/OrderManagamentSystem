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
    /// Get all active products
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetAllActiveProductsAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        return product != null ? Ok(product) : NotFound($"Product with ID {id} not found");
    }

    /// <summary>
    /// Get products by multiple IDs
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> GetProductsBatch([FromBody] List<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds == null || productIds.Count == 0)
            return BadRequest("Product IDs cannot be empty");

        var products = await _productService.GetProductsByIdsAsync(productIds, cancellationToken);
        return Ok(products);
    }
}
