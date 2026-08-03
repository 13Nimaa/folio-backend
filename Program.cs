using BooksProject.Data;
using BooksProject.Endpoints;
using GameStore.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.AddAppStoreDb();
builder.Services.AddValidation();
var app = builder.Build();

app.MigrateDb();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();

app.MapBookEndpoints();
app.MapGenresEndpoint();


app.Run();

