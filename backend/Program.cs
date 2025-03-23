using PriceFeedService.Hubs;
using PriceFeedService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PriceHub>("/ws/prices");

// Start Binance connection and broadcast from server
var hubContext = app.Services.GetRequiredService<IHubContext<PriceHub>>();
PriceService.Start(hubContext);

app.Run();
