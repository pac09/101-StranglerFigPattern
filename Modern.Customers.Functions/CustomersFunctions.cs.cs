using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Modern.Customers.Functions;

public class CustomersFunctions
{
    private readonly ILogger _logger;

    // In-memory “modern” customer store
    private static readonly List<Customer> Customers =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Alice (modernised)"),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Charlie (modernised)")
    ];

    public CustomersFunctions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CustomersFunctions>();
    }

    // GET /customers
    [Function("GetCustomers")]
    public async Task<HttpResponseData> GetCustomers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")]
        HttpRequestData req)
    {
        _logger.LogInformation("GetCustomers invoked.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        var dto = Customers.Select(c => new CustomerDto(c.Id, c.Name));

        await response.WriteAsJsonAsync(dto);
        return response;
    }

    // GET /customers/{id}
    [Function("GetCustomerById")]
    public async Task<HttpResponseData> GetCustomerById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id:guid}")]
        HttpRequestData req,
        string id)
    {
        _logger.LogInformation("GetCustomerById invoked for {Id}", id);

        if (!Guid.TryParse(id, out var customerId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid GUID.");
            return bad;
        }

        var customer = Customers.SingleOrDefault(c => c.Id == customerId);
        if (customer is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(new CustomerDto(customer.Id, customer.Name));
        return ok;
    }

    private record Customer(Guid Id, string Name);
    private record CustomerDto(Guid Id, string Name);
}
