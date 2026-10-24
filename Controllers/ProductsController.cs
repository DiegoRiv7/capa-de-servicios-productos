using Microsoft.AspNetCore.Mvc;
using backend_II.Models;
using backend_II.Services;

namespace backend_II.Controllers
{
    // El controlador ya no conoce el repositorio ni la base de datos:
    // recibe la petición HTTP, se la pasa a la capa de servicios y devuelve la respuesta.
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: api/products
        [HttpGet]
        public Task<GeneralResponse<List<Product>>> GetAll()
            => _productService.GetAllActiveAsync();

        // GET: api/products/{sku}
        [HttpGet("{sku}")]
        public Task<GeneralResponse<Product>> GetByRoute([FromRoute] string sku)
            => _productService.GetBySkuAsync(sku);

        // GET: api/products/search?sku=XYZ
        [HttpGet("search")]
        public Task<GeneralResponse<Product>> GetByQuery([FromQuery] string? sku)
            => _productService.GetBySkuAsync(sku);

        // POST: api/products/search   body: { "sku": "XYZ" }
        [HttpPost("search")]
        public Task<GeneralResponse<Product>> GetByBody([FromBody] ProductSearchRequest request)
            => _productService.GetBySkuAsync(request?.Sku);
    }
}
