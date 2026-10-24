# Consulta de productos con capa de servicios

**Desarrollo de Software Backend 2** · CESUN Universidad
Alumno: **Rivera Cordova Diego Jafet**
Actividad: *Modificar el programa de consulta de productos para incorporar la capa de servicios.*

API REST en **ASP.NET Core 9** con **SQL Server** que consulta el catálogo de productos de la base `e_commerce` vista en clase. Se parte del proyecto `backend_II` y se reorganiza para que la lógica de negocio viva en una **capa de servicios** separada del controlador y del acceso a datos.

---

## 1. ¿Qué es la capa de servicios y por qué agregarla?

En la versión anterior el controlador hacía todo: recibía la petición HTTP, validaba el SKU, llamaba al repositorio, decidía qué mensaje devolver y atrapaba las excepciones. Ese mismo bloque de código estaba copiado en cada uno de los cuatro endpoints.

La capa de servicios es la clase que se coloca **entre el controlador y el repositorio** y se queda con esa lógica. Así cada capa tiene una sola responsabilidad:

| Capa | Qué hace | Qué NO hace |
|---|---|---|
| **Controlador** | Recibe la petición HTTP y devuelve la respuesta. | No valida, no conoce la base de datos. |
| **Servicio** | Reglas de negocio: valida, normaliza, decide mensajes, maneja errores. | No sabe de HTTP ni de SQL. |
| **Repositorio** | Ejecuta el procedimiento almacenado y convierte filas en objetos. | No toma decisiones de negocio. |

Beneficios concretos en este proyecto:

- La validación del SKU y el manejo de errores están **una sola vez** en vez de cuatro.
- El controlador se puede leer completo en pantalla: cada endpoint es una línea.
- El servicio depende de la **interfaz** `IProductRepository`, no de SQL Server. Se puede cambiar el motor o usar un repositorio falso en pruebas sin tocar la lógica.
- Si el día de mañana hay otra entrada (una app de consola, un worker, gRPC) reutiliza el mismo `ProductService`.

---

## 2. Arquitectura

```
          HTTP
           │
           ▼
┌─────────────────────────┐
│   ProductsController    │   Controllers/ProductsController.cs
│   api/products/...      │   Solo delega al servicio.
└───────────┬─────────────┘
            │ IProductService
            ▼
┌─────────────────────────┐
│     ProductService      │   Services/ProductService.cs
│  valida · normaliza ·   │   Lógica de negocio.
│  mensajes · errores     │
└───────────┬─────────────┘
            │ IProductRepository
            ▼
┌─────────────────────────┐
│    ProductRepository    │   Database/ProductRepository.cs
│  sp_GetAllActiveProducts│   Microsoft.Data.SqlClient
└───────────┬─────────────┘
            │
            ▼
      SQL Server · e_commerce
```

Las dependencias se inyectan por interfaz en `Program.cs`:

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```

---

## 3. Antes y después

**Antes** (fragmento real del controlador, repetido en cada endpoint):

```csharp
[HttpGet("search")]
public async Task<GeneralResponse<Product>> GetByQuery([FromQuery] string sku)
{
    if (string.IsNullOrWhiteSpace(sku))
        return new GeneralResponse<Product> { Success = false, Message = "El parámetro 'sku' en la URL es obligatorio." };

    try
    {
        var product = await _repository.GetProductBySkuAsync(sku);
        if (product == null)
            return new GeneralResponse<Product> { Success = false, Message = "Producto no encontrado o inactivo." };

        return new GeneralResponse<Product> { Success = true, Message = "Producto encontrado.", Data = product };
    }
    catch (Exception ex)
    {
        return new GeneralResponse<Product> { Success = false, Message = ex.Message };
    }
}
```

**Después** (controlador):

```csharp
[HttpGet("search")]
public Task<GeneralResponse<Product>> GetByQuery([FromQuery] string? sku)
    => _productService.GetBySkuAsync(sku);
```

**Después** (servicio, donde ahora vive la lógica):

```csharp
public async Task<GeneralResponse<Product>> GetBySkuAsync(string? sku)
{
    if (string.IsNullOrWhiteSpace(sku))
        return Fail<Product>("El SKU es obligatorio.");

    var normalizedSku = sku.Trim().ToUpperInvariant();

    try
    {
        var product = await _repository.GetProductBySkuAsync(normalizedSku);

        if (product == null)
            return Fail<Product>($"No se encontró un producto activo con el SKU '{normalizedSku}'.");

        return Ok(product, "Producto encontrado.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al buscar el producto con SKU {Sku}.", normalizedSku);
        return Fail<Product>("Ocurrió un error al consultar el producto.");
    }
}
```

Dos mejoras que se aprovecharon al mover la lógica:

- El SKU se **normaliza** (`Trim` + mayúsculas) antes de ir a la base.
- Cuando falla SQL Server, el cliente recibe un mensaje genérico y el detalle de la excepción se escribe en el **log** con `ILogger`, en lugar de exponerse en la respuesta.

---

## 4. Archivos de la actividad

| Archivo | Estado | Qué es |
|---|---|---|
| `Services/IProductService.cs` | nuevo | Contrato de la capa de servicios. |
| `Services/ProductService.cs` | nuevo | Implementación: validación, normalización, mensajes, manejo de errores. |
| `Database/IProductRepository.cs` | nuevo | Contrato del acceso a datos. |
| `Database/ProductRepository.cs` | modificado | Ahora implementa `IProductRepository`. Sin cambios en las consultas. |
| `Controllers/ProductsController.cs` | modificado | Recibe `IProductService`; cada endpoint delega en una línea. |
| `Program.cs` | modificado | Registro de ambas capas por interfaz. |
| `Models/GeneralResponse.cs`, `Models/Product.cs`, `Models/ProductSearchRequest.cs` | sin cambios | Modelos del proyecto de clase. |
| `DatabaseScripts/esquema_inicial.sql`, `DatabaseScripts/sp_GetAllActiveProducts.sql` | sin cambios | Base `e_commerce` y procedimiento almacenado. |

---

## 5. Endpoints

Todas las respuestas usan el formato `GeneralResponse` del proyecto: `success`, `message`, `data`.

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `api/products` | Lista los productos activos. |
| GET | `api/products/{sku}` | Busca un producto por SKU en la ruta. |
| GET | `api/products/search?sku=XYZ` | Busca un producto por SKU en la query string. |
| POST | `api/products/search` · body `{ "sku": "XYZ" }` | Busca un producto por SKU en el cuerpo. |

Ejemplos de respuesta:

```json
// GET api/products/search      (sin sku)
{ "success": false, "message": "El SKU es obligatorio.", "data": null }

// GET api/products/sku-que-no-existe
{ "success": false, "message": "No se encontró un producto activo con el SKU 'SKU-QUE-NO-EXISTE'.", "data": null }

// GET api/products/ABC-001
{ "success": true, "message": "Producto encontrado.",
  "data": { "productId": 1, "sku": "ABC-001", "name": "...", "description": "...", "price": 199.99, "stockQuantity": 10 } }
```

---

## 6. Cómo ejecutar

1. Crear la base con `DatabaseScripts/esquema_inicial.sql` y luego `DatabaseScripts/sp_GetAllActiveProducts.sql` en SQL Server.
2. Crear un archivo `.env` en la raíz del proyecto (no se sube al repo) con la cadena de conexión:

   ```
   ConnectionStrings__DefaultConnection=Server=localhost;Database=e_commerce;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;
   ```

3. Ejecutar:

   ```bash
   dotnet run --launch-profile http
   ```

4. Probar en `http://localhost:5161/api/products`.

---

## 7. Otros módulos en el repositorio

El proyecto también conserva la actividad anterior de la materia, **APIs que consumen APIs**: el endpoint `consult/pokemons` que consume [PokeAPI](https://pokeapi.co) con `HttpClient` e `IHttpClientFactory` (`Services/PokeApiService.cs`, `Controllers/PokemonsController.cs`). Ese módulo ya seguía el esquema Controller → Service, y fue la referencia para llevar el mismo orden a la consulta de productos. Su documentación está en el repositorio [apis-que-consumen-apis](https://github.com/DiegoRiv7/apis-que-consumen-apis).
