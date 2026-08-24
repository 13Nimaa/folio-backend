using BooksProject.Authentication;
using BooksProject.Data;
using BooksProject.Endpoints;
using BooksProject.Handlers;
using FluentValidation;
using GameStore.Api.Endpoints;
using BooksProject.Configuration;
using BooksProject.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:3000"];

        policy
            .SetIsOriginAllowed(origin =>
                origins.Any(o => origin.TrimEnd('/') == o.TrimEnd('/')))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddScoped<IImageService, CloudinaryImageService>();
builder.AddAppStoreDb();
builder.AddJwtAuthentication();
builder.Services.AddValidation();
builder.Services.AddSignalR();
builder.AddAuthRateLimiting();
// Registers every AbstractValidator<T> in this assembly.
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();

    app.MigrateDb();


// Must run first so it can catch exceptions from everything after it.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseCors("frontend");
app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapAuthEndpoints();
app.MapBookEndpoints();
app.MapGenresEndpoint();
app.MapWishlistEndpoints();
app.MapUserEndpoints();
app.MapOrderEndpoints();
app.MapConversationEndpoints();

// Real-time chat: the JS client negotiates /hubs/chat before opening the
// transport — without this the negotiate request 404s and chat goes offline.
app.MapHub<BooksProject.Hubs.ChatHub>("/hubs/chat");

app.Run();
