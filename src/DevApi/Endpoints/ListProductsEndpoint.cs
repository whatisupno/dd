using Dapper;
using FastEndpoints;
using Microsoft.Data.SqlClient;

namespace DevApi.Endpoints;

public record ProductDto(int Id, string Name, decimal ListPrice);

/// <summary>GET /products - first 50 products by name (AdventureWorks Production.Product).</summary>
public class ListProductsEndpoint(IConfiguration config)
    : EndpointWithoutRequest<List<ProductDto>>
{
    public override void Configure()
    {
        Get("/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(config.GetConnectionString("Default"));

        var rows = await conn.QueryAsync<ProductDto>(
            """
            SELECT TOP (50)
                   ProductID AS Id,
                   Name,
                   ListPrice
            FROM   Production.Product
            ORDER  BY ListPrice
            """);

        await Send.OkAsync(rows.ToList(), cancellation: ct);
    }
}
