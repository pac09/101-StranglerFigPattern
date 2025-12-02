using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("legacy", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
});

builder.Services.AddHttpClient("modernFunctions", client =>
{
    // Azure Functions host running locally
    client.BaseAddress = new Uri("http://localhost:7071");
});

var app = builder.Build();

app.MapGet("/", () => "Gateway running. Call /customers or /customers/{id}.");

// ------------------------------------------------------------------------
// GET /customers – AZURE FUNCTIONS (modern side)
// ------------------------------------------------------------------------
app.MapGet("/customers", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("modernFunctions");

    var customers = await client
        .GetFromJsonAsync<List<BackendCustomerDto>>("/customers");

    if (customers is null)
        return Results.Problem("Modern (Functions) backend did not return data.");

    var response = customers
        .Select(c => new CustomerResponse(c.Id, c.Name, "modern-functions"));

    return Results.Ok(response);
});

// ------------------------------------------------------------------------
// GET /customers/{id} 
// ------------------------------------------------------------------------
app.MapGet("/customers/{id:guid}",
    async (Guid id, IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("legacy");

        var customer = await client
            .GetFromJsonAsync<BackendCustomerDto>($"/customers/{id}");

        if (customer is null)
            return Results.NotFound();

        var response = new CustomerResponse(customer.Id, customer.Name, "legacy");
        return Results.Ok(response);
    });

// ------------------------------------------------------------------------
// POST /customers – STILL LEGACY WRITE PATH
// ------------------------------------------------------------------------
app.MapPost("/customers",
    async (CreateCustomerRequest request, IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("legacy");

        var backendResponse = await client
            .PostAsJsonAsync("/customers", request);

        if (!backendResponse.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)backendResponse.StatusCode);
        }

        var customer = await backendResponse.Content
            .ReadFromJsonAsync<BackendCustomerDto>();

        if (customer is null)
            return Results.Problem("Legacy backend did not return a customer.");

        var response = new CustomerResponse(customer.Id, customer.Name, "legacy");

        return Results.Created($"/customers/{customer.Id}", response);
    });

app.Run("http://localhost:5000");

record BackendCustomerDto(Guid Id, string Name);
record CustomerResponse(Guid Id, string Name, string Backend);
record CreateCustomerRequest(string Name);