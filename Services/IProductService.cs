using backend_II.Models;

namespace backend_II.Services
{
    // Contrato de la capa de servicios de productos.
    // El controlador solo conoce esta interfaz; no sabe cómo ni de dónde salen los datos.
    public interface IProductService
    {
        Task<GeneralResponse<List<Product>>> GetAllActiveAsync();
        Task<GeneralResponse<Product>> GetBySkuAsync(string? sku);
    }
}
