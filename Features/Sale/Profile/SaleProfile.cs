using AutoMapper;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Sale.DTO;

namespace venta_stock_webapi.Sale.Profile
{
    public class SaleProfile : AutoMapper.Profile
    {
        public SaleProfile()
        {
            // ===================================
            // Mapeos para Venta (Ventum)
            // ===================================

            // CreateSaleDTO → Ventum (no se usa directamente, se crea manualmente en el servicio)
            // Pero lo dejamos por si lo necesitas en el futuro
            CreateMap<CreateSaleDTO, Ventum>()
                .ForMember(dest => dest.IdVenta, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoVenta, opt => opt.Ignore())
                .ForMember(dest => dest.Fecha, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Total, opt => opt.Ignore())
                .ForMember(dest => dest.IdEstado, opt => opt.MapFrom(src => 2)); // 2 = Completada

            // Ventum → SaleResponseDTO
            CreateMap<Ventum, SaleResponseDTO>()
                .ForMember(dest => dest.Cliente, opt => opt.MapFrom(src =>
                    src.IdClienteNavigation.Nombre ?? src.IdClienteNavigation.RazonSocial ?? "N/A"))
                .ForMember(dest => dest.ClienteDni, opt => opt.MapFrom(src =>
                    src.IdClienteNavigation.Dni ?? src.IdClienteNavigation.Cuit ?? "N/A"))
                .ForMember(dest => dest.ClienteTelefono, opt => opt.MapFrom(src =>
                    src.IdClienteNavigation.Telefono ?? "N/A"))
                .ForMember(dest => dest.VendedorNombre, opt => opt.MapFrom(src =>
                    src.IdUsuarioNavigation.Nombre + " " + src.IdUsuarioNavigation.Apellido))
                .ForMember(dest => dest.MedioPago, opt => opt.MapFrom(src =>
                    src.IdMedioPagoNavigation.MedioPago1 ?? "N/A"))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src =>
                    src.IdEstadoNavigation.Estado1 ?? "N/A"))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.DetalleVenta));

            // Ventum → SaleListDTO (para el historial)
            CreateMap<Ventum, SaleListDTO>()
                .ForMember(dest => dest.Cliente, opt => opt.MapFrom(src => 
                    src.IdClienteNavigation.Nombre ?? src.IdClienteNavigation.RazonSocial ?? "N/A"))
                .ForMember(dest => dest.MedioPago, opt => opt.MapFrom(src => 
                    src.IdMedioPagoNavigation.MedioPago1 ?? "N/A"))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => 
                    src.IdEstadoNavigation.Estado1 ?? "N/A"))
                .ForMember(dest => dest.Vendedor, opt => opt.MapFrom(src => 
                    src.IdUsuarioNavigation.Nombre + " " + src.IdUsuarioNavigation.Apellido));

            // ===================================
            // Mapeos para Detalle de Venta
            // ===================================

            // SaleItemDTO → DetalleVentum (no se usa directamente, se crea manualmente)
            CreateMap<SaleItemDTO, DetalleVentum>()
                .ForMember(dest => dest.IdVenta, opt => opt.Ignore())
                .ForMember(dest => dest.IdProducto, opt => opt.MapFrom(src => src.IdProducto))
                .ForMember(dest => dest.Cantidad, opt => opt.MapFrom(src => src.Cantidad))
                .ForMember(dest => dest.PrecioVenta, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore());

            // DetalleVentum → SaleItemDetailDTO
            CreateMap<DetalleVentum, SaleItemDetailDTO>()
                .ForMember(dest => dest.NombreProducto, opt => opt.MapFrom(src =>
                    src.IdProductoNavigation.Nombre ?? "N/A"))
                .ForMember(dest => dest.MarcaProducto, opt => opt.MapFrom(src =>
                    src.IdProductoNavigation.Marca ?? "N/A"))
                .ForMember(dest => dest.IdUnidadMedida, opt => opt.MapFrom(src =>
                    src.IdProductoNavigation.IdUnidadMedida))
                .ForMember(dest => dest.PrecioUnitario, opt => opt.MapFrom(src => src.PrecioVenta ?? 0))
                .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.SubTotal ?? 0));
        }
    }
}