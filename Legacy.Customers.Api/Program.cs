using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var customers = new List<Customer>
{
    new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Alice (legacy)"),
    new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Bob (legacy)")
};

app.MapGet("/", () => "Legacy customers API");

app.MapGet("/customers", () =>
    customers.Select(c => new CustomerDto(c.Id, c.Name)));

app.MapGet("/customers/{id:guid}", (Guid id) =>
{
    var customer = customers.SingleOrDefault(c => c.Id == id);
    return customer is null
        ? Results.NotFound()
        : Results.Ok(new CustomerDto(customer.Id, customer.Name));
});

app.MapPost("/customers", (CreateCustomerRequest request) =>
{
    var customer = new Customer(Guid.NewGuid(), request.Name + " (legacy)");
    customers.Add(customer);

    return Results.Created(
        $"/customers/{customer.Id}",
        new CustomerDto(customer.Id, customer.Name));
});

app.Run("http://localhost:5001");

record Customer(Guid Id, string Name);
record CustomerDto(Guid Id, string Name);
record CreateCustomerRequest(string Name);
