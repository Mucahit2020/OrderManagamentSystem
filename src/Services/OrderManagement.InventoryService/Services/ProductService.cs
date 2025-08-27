using AutoMapper;
using OrderManagement.InventoryService.DTOs;
using OrderManagement.InventoryService.Interfaces;

namespace OrderManagement.InventoryService.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, IMapper mapper, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product != null ? _mapper.Map<ProductDto>(product) : null;
    }

    public async Task<List<ProductDto>> GetAllActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllActiveAsync(cancellationToken);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> GetProductsByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetByIdsAsync(ids, cancellationToken);
        return _mapper.Map<List<ProductDto>>(products);
    }
}