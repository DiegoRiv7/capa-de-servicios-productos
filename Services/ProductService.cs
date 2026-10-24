using backend_II.Database;
using backend_II.Models;

namespace backend_II.Services
{
    // Capa de servicios: aquí vive la lógica de negocio de productos.
    //
    //   Controller  ->  Service  ->  Repository  ->  SQL Server (sp_GetAllActiveProducts)
    //
    // El controlador solo recibe la petición HTTP y devuelve lo que el servicio le entrega.
    // El repositorio solo ejecuta el procedimiento almacenado y mapea filas a objetos.
    // Todo lo que está en medio (validar, normalizar, decidir el mensaje, manejar errores)
    // es responsabilidad de esta clase, y queda en un solo lugar en vez de repetirse
    // en cada endpoint.
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository repository, ILogger<ProductService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GeneralResponse<List<Product>>> GetAllActiveAsync()
        {
            try
            {
                var products = await _repository.GetAllActiveProductsAsync();

                return Ok(products, products.Count == 0
                    ? "No hay productos activos."
                    : $"Se obtuvieron {products.Count} productos activos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los productos activos.");
                return Fail<List<Product>>("Ocurrió un error al consultar los productos.");
            }
        }

        public async Task<GeneralResponse<Product>> GetBySkuAsync(string? sku)
        {
            // Regla de negocio: el SKU es obligatorio y se compara sin importar
            // espacios ni mayúsculas/minúsculas.
            if (string.IsNullOrWhiteSpace(sku))
            {
                return Fail<Product>("El SKU es obligatorio.");
            }

            var normalizedSku = sku.Trim().ToUpperInvariant();

            try
            {
                var product = await _repository.GetProductBySkuAsync(normalizedSku);

                if (product == null)
                {
                    return Fail<Product>($"No se encontró un producto activo con el SKU '{normalizedSku}'.");
                }

                return Ok(product, "Producto encontrado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar el producto con SKU {Sku}.", normalizedSku);
                return Fail<Product>("Ocurrió un error al consultar el producto.");
            }
        }

        // Helpers para construir el GeneralResponse siempre con la misma forma.
        private static GeneralResponse<T> Ok<T>(T data, string message) =>
            new() { Success = true, Message = message, Data = data };

        private static GeneralResponse<T> Fail<T>(string message) =>
            new() { Success = false, Message = message, Data = default };
    }
}
