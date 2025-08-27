using AutoMapper;
using OrderManagement.InvoiceService.DTOs;
using OrderManagement.InvoiceService.Models;

namespace OrderManagement.InvoiceService.Mappings;

public class InvoiceMappingProfile : Profile
{
    public InvoiceMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Invoice, InvoiceDto>();
        CreateMap<InvoiceItem, InvoiceItemDto>();
    }
}
