using AutoMapper;
using OrderManagement.Common.Enums;
using OrderManagement.Common.Events;
using OrderManagement.Common.Models;
using OrderManagement.OrderService.DTOs;
using OrderManagement.OrderService.Models;

namespace OrderManagement.OrderService.Mappings;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Order, OrderResponse>();
        CreateMap<OrderItem, OrderItemResponse>();

        // DTO to Entity mappings
        CreateMap<CreateOrderRequest, Order>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => OrderStatus.Created))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Items.Sum(i => i.Quantity * i.UnitPrice)))
            .ForMember(dest => dest.IdempotencyKey, opt => opt.Ignore()); // Set separately

        CreateMap<CreateOrderItemRequest, OrderItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.OrderId, opt => opt.Ignore()); // Set by parent

        // Entity to Event mappings
        CreateMap<Order, OrderCreated>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice));

        // Request DTO to Event DTO mapping
        CreateMap<CreateOrderItemRequest, OrderItemDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice));
    }
}
