using backend_II.Models;

namespace backend_II.Database
{
    // Contrato de acceso a datos. El servicio depende de esta interfaz, no de SQL Server,
    // así el repositorio puede sustituirse (otro motor, un mock en pruebas) sin tocar la lógica.
    public interface IProductRepository
    {
        Task<List<Product>> GetAllActiveProductsAsync();
        Task<Product?> GetProductBySkuAsync(string sku);
    }
}
