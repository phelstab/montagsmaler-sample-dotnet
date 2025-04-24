using Pictionary.Server.Hubs;
using Pictionary.Server.Services;
using Pictionary.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins("http://localhost:5088")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
});

var app = builder.Build();

app.UseCors("CorsPolicy");
app.MapHub<PictionaryHub>(Constants.HubUrl);

app.Run();
