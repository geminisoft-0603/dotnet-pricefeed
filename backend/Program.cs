using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.WithOrigins("http://localhost:8080")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
  });
});

var app = builder.Build();

app.UseRouting();
app.UseCors(); 
app.UseAuthorization();

app.MapControllers();
app.MapHub<PriceHub>("/ws/prices");

var hubContext = app.Services.GetRequiredService<IHubContext<PriceHub>>();
await PriceService.Start(hubContext);

app.Run();
