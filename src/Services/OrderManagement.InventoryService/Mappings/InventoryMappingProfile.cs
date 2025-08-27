using AutoMapper;
using OrderManagement.InventoryService.DTOs;
using OrderManagement.InventoryService.Models;

namespace OrderManagement.InventoryService.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Product, ProductDto>();
        CreateMap<StockMovement, StockMovementDto>();
    }
}