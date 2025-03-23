using System.Collections.Concurrent;
using Websocket.Client;
using System.Reactive.Linq;
using System.Text.Json;
using PriceFeedService.Models;

public static class PriceService
{
  private static readonly ConcurrentDictionary<string, PriceUpdate> latestPrices = new();
  private static WebsocketClient _client;

  public static void Start()
  {
    var url = new Uri("wss://stream.binance.com:443/ws/btcusdt@aggTrade");
    _client = new WebsocketClient(url);
    _client.MessageReceived
      .Where(msg => msg.Text != null)
      .Subscribe(msg =>
      {
        var json = JsonDocument.Parse(msg.Text);
        var price = decimal.Parse(json.RootElement.GetProperty("p").GetString());
        latestPrices["BTCUSDT"] = new PriceUpdate
        {
          Symbol = "BTCUSDT",
          Price = price,
          Timestamp = DateTime.UtcNow
        };
        Console.WriteLine($"[Price Update] BTCUSDT = {price}");
      });

    _client.Start().Wait();

    _client.Send(JsonSerializer.Serialize(new
    {
      method = "SUBSCRIBE",
      @params = new[] { "btcusdt@aggTrade" },
      id = 1
    }));
  }

  public static PriceUpdate GetLatestPrice(string symbol)
  {
    latestPrices.TryGetValue(symbol.ToUpper(), out var price);
    return price;
  } 
}
