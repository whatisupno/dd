using Dapper;
using FastEndpoints;
using Microsoft.Data.SqlClient;

namespace DevApi.Endpoints;

//[BusinessEntityID]
//[AccountNumber]
//      ,[Name]
//     ,[CreditRating]
//      ,[PreferredVendorStatus]
//      ,[ActiveFlag]
//      ,[PurchasingWebServiceURL]
//      ,[ModifiedDate]
public record VendorDto(int BusinessEntityID, string AccountNumber, string Name, 
byte CreditRating, bool PreferredVendorStatus, bool ActiveFlag, string PurchasingWebServiceURL,
DateTime ModifiedDate);

/// <summary>GET /vendors - list all vendors.</summary>
public class ListVendorsEndpoint(IConfiguration config)
    : EndpointWithoutRequest<List<VendorDto>>
{
    public override void Configure()
    {
        Get("/vendors");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(config.GetConnectionString("Default"));

        var rows = await conn.QueryAsync<VendorDto>(
"""
            SELECT TOP (1000) [BusinessEntityID]
            ,[AccountNumber]
            ,[Name]
            ,[CreditRating]
            ,[PreferredVendorStatus]
            ,[ActiveFlag]
            ,[PurchasingWebServiceURL]
            ,[ModifiedDate]
            FROM [AdventureWorks2025].[Purchasing].[Vendor]
            """);

        await Send.OkAsync(rows.ToList(), cancellation: ct);
    }
}

public class GetVendorRequest

{

    public int BusinessEntityID { get; set; }

}



/// <summary>GET /vendors/{BusinessEntityID} - one vendor by ID.</summary>

public class GetVendorEndpoint(IConfiguration config)

    : Endpoint<GetVendorRequest, VendorDto>

{

    public override void Configure()

    {

        Get("/vendors/{BusinessEntityID}");

        AllowAnonymous();

    }



    public override async Task HandleAsync(GetVendorRequest req, CancellationToken ct)

    {

        await using var conn = new SqlConnection(config.GetConnectionString("Default"));



        var vendor = await conn.QuerySingleOrDefaultAsync<VendorDto>(

            """

            SELECT BusinessEntityID, AccountNumber, Name, CreditRating,

                   PreferredVendorStatus, ActiveFlag, PurchasingWebServiceURL, ModifiedDate

            FROM   Purchasing.Vendor

            WHERE  BusinessEntityID = @BusinessEntityID

            """,

            new { req.BusinessEntityID });



        if (vendor is null)

        {

            await Send.NotFoundAsync(ct);

            return;

        }



        await Send.OkAsync(vendor, ct);

    }

}


