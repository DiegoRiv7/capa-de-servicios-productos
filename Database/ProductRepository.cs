using System.Data;
using Microsoft.Data.SqlClient;
using backend_II.Models;

namespace backend_II.Database
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<List<Product>> GetAllActiveProductsAsync()
        {
            var products = new List<Product>();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_GetAllActiveProducts", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    command.Parameters.AddWithValue("@method", "Products");
                    command.Parameters.AddWithValue("@isActive", true);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetName(0) == "code") 
                            {
                                throw new Exception(reader["message"].ToString());
                            }

                            products.Add(new Product
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                SKU = reader["SKU"].ToString() ?? string.Empty,
                                Name = reader["Name"].ToString() ?? string.Empty,
                                Description = reader["Description"] != DBNull.Value
                                                ? reader["Description"].ToString() ?? string.Empty 
                                                : string.Empty,
                                Price = Convert.ToDecimal(reader["Price"]),
                                StockQuantity = Convert.ToInt32(reader["StockQuantity"])
                            });
                        }
                    }
                }
            }

            return products;
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_GetAllActiveProducts", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@method", "GetProductBySKU");
                    command.Parameters.AddWithValue("@SKU", sku);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader.GetName(0) == "code") 
                            {
                                return null;
                            }

                            return new Product
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                SKU = reader["SKU"].ToString() ?? string.Empty,
                                Name = reader["Name"].ToString() ?? string.Empty,
                                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() ?? string.Empty : string.Empty,
                                Price = Convert.ToDecimal(reader["Price"]),
                                StockQuantity = Convert.ToInt32(reader["StockQuantity"])
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}
