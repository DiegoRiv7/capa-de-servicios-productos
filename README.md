# Capa de servicios en la consulta de productos

Proyecto de la materia **Desarrollo de Software Backend 2** (CESUN Universidad).
Alumno: **Rivera Cordova Diego Jafet**.

Se modifica el programa de consulta de productos para incorporar una **capa de servicios** entre el controlador y el repositorio:

```
HTTP  ->  ProductsController  ->  IProductService / ProductService  ->  IProductRepository / ProductRepository  ->  SQL Server
```

| Capa | Archivo | Responsabilidad |
|---|---|---|
| Controlador | `Controllers/ProductsController.cs` | Recibe la petición HTTP y devuelve lo que entrega el servicio. Ya no conoce la base de datos. |
| Servicio | `Services/IProductService.cs`, `Services/ProductService.cs` | Lógica de negocio: valida y normaliza el SKU, decide los mensajes, maneja errores y los registra con `ILogger`. |
| Repositorio | `Database/IProductRepository.cs`, `Database/ProductRepository.cs` | Ejecuta `sp_GetAllActiveProducts` y mapea las filas a `Product`. |

Las dependencias se registran por interfaz en `Program.cs` con `AddScoped`, así que el controlador recibe `IProductService` y el servicio recibe `IProductRepository`.

## Qué cambió respecto a la versión anterior

- Antes cada endpoint repetía el `try/catch`, la validación del SKU y la construcción del `GeneralResponse`. Ahora eso está una sola vez en `ProductService`.
- Los cuatro endpoints de productos (`GET api/products`, `GET api/products/{sku}`, `GET api/products/search?sku=`, `POST api/products/search`) quedaron en una línea cada uno.
- El SKU se normaliza (`Trim` + mayúsculas) antes de consultar.
- Si falla la base de datos, el cliente recibe un mensaje genérico y el detalle de la excepción se escribe en el log, no en la respuesta.

---

# APIS que consumen APIS — endpoint `consult/pokemons`

Proyecto de la materia **Desarrollo de Software Backend 2** (CESUN Universidad).
Alumno: **Rivera Cordova Diego Jafet**.

Se parte del proyecto compartido en clase (`backend_II`, ASP.NET Core 9) y se agrega un endpoint nuevo que consume la API pública [PokeAPI](https://pokeapi.co) usando `HttpClient` como lo recomienda [el artículo de NetMentor](https://www.netmentor.es/entrada/en/implementar-httpclient).

## Endpoints nuevos

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `consult/pokemons?limit=20&offset=0` | Listado paginado de pokémon (`limit` entre 1 y 100) |
| GET | `consult/pokemons/{nombre o id}` | Detalle: tipos, habilidades, altura, peso e imagen |

Las respuestas usan el mismo formato que el resto del proyecto (`GeneralResponse`): `success`, `message`, `data`.

## Archivos de la actividad

| Archivo | Qué es |
|---|---|
| `Program.cs` | `AddHttpClient("PokeApi", ...)` con la URL base y registro de `PokeApiService` |
| `Services/PokeApiService.cs` | Consume PokeAPI con `IHttpClientFactory`, `GetFromJsonAsync` y `ReadFromJsonAsync` |
| `Controllers/PokemonsController.cs` | Controlador nuevo en la ruta `consult/pokemons` |
| `Models/PokeApi/*.cs` | Forma del JSON que devuelve PokeAPI |
| `Models/Pokemon.cs` | Lo que devuelve nuestra API |
| `appsettings.json` | URL base de PokeAPI |

## Cómo ejecutar

```bash
dotnet run --launch-profile http
```

Queda en `http://localhost:5161`. Ejemplos:

```
http://localhost:5161/consult/pokemons?limit=5&offset=150
http://localhost:5161/consult/pokemons/pikachu
```

El endpoint de pokémon no necesita base de datos ni archivo `.env`; los de productos (`api/products`) sí requieren la cadena de conexión a SQL Server del proyecto original.

## Puntos clave del uso de HttpClient

- No se hace `new HttpClient()` por petición: eso agota los sockets del sistema.
- El cliente se registra una vez con nombre (`AddHttpClient`) y se obtiene con `IHttpClientFactory.CreateClient("PokeApi")`.
- La deserialización va directo a objetos C# con `GetFromJsonAsync<T>`; en el detalle se usa `GetAsync` para distinguir el 404 (pokémon inexistente) y luego `ReadFromJsonAsync<T>`.
