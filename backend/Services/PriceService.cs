using System.Collections.Concurrent;
using Websocket.Client;
using System.Reactive.Linq;
using System.Text.Json;
using PriceFeedService.Models;
using Microsoft.AspNetCore.SignalR;
using PriceFeedService.Hubs;
using System.Text;

namespace PriceFeedService.Services
{
  public static class PriceService
  {
    private static readonly ConcurrentDictionary<string, PriceUpdate> LatestPrices = new();
    private static IHubContext<PriceHub>? _hubContext;

    public static void Start(IHubContext<PriceHub> hubContext)
    {
      _hubContext = hubContext;
      StartBinance();
      StartTiingo();
    }

    private static void StartBinance()
    {
      var url = new Uri("wss://stream.binance.com:443/ws/btcusdt@aggTrade");
      var client = new WebsocketClient(url);

      client.MessageReceived
        .Where(msg => msg.Text != null)
        .Subscribe(async msg =>
        {
          try
          {
            var json = JsonDocument.Parse(msg.Text!);

            if (json.RootElement.TryGetProperty("p", out var priceElement))
            {
              if (_hubContext == null) return;

              var price = decimal.Parse(priceElement.GetString()!);
              var update = new PriceUpdate
              {
                Symbol = "BTCUSDT",
                Price = price,
                Timestamp = DateTime.UtcNow
              };
              LatestPrices["BTCUSDT"] = update;
              await _hubContext.Clients.Group("BTCUSDT").SendAsync("ReceivePrice", update);
              Console.WriteLine($"[Binance] BTCUSDT = {price}");
            }
            else
            {
              Console.WriteLine("[Binance] Skipped message: no 'p' property.");
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine($"[Binance] Error parsing message: {ex.Message}");
          }
        });

      client.Start().Wait();

      client.Send(JsonSerializer.Serialize(new
      {
        method = "SUBSCRIBE",
        @params = new[] { "btcusdt@aggTrade" },
        id = 1
      }));
    }

    private static void StartTiingo()
    {
      var tiingoToken = Environment.GetEnvironmentVariable("TIINGO_API_KEY") ?? string.Empty;
      if (string.IsNullOrWhiteSpace(tiingoToken))
      {
        Console.WriteLine("[Tiingo] Token not provided. Skipping Tiingo WebSocket connection.");
        return;
      }

      var url = new Uri("wss://api.tiingo.com/fx");
      var client = new WebsocketClient(url)
      {
        IsReconnectionEnabled = true
      };

      client.MessageReceived
        .Where(msg => msg.Text != null)
        .Subscribe(async msg =>
        {
          try
          {
            if (_hubContext == null) return;

            var json = JsonDocument.Parse(msg.Text!);
            var root = json.RootElement;

            if (root.TryGetProperty("messageType", out var typeElem) &&
                typeElem.GetString() == "A" &&
                root.TryGetProperty("data", out var dataElem) &&
                dataElem.ValueKind == JsonValueKind.Array &&
                dataElem.GetArrayLength() >= 8)
            {
              var symbol = dataElem[1].GetString()!.ToUpper();
              var price = dataElem[5].GetDecimal(); // midPrice from array index 5
              var timestamp = DateTime.Parse(dataElem[2].GetString()!);

              var update = new PriceUpdate
              {
                Symbol = symbol,
                Price = price,
                Timestamp = timestamp
              };

              LatestPrices[symbol] = update;
              await _hubContext.Clients.Group(symbol).SendAsync("ReceivePrice", update);
              Console.WriteLine($"[Tiingo] {symbol} = {price}");
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine($"[Tiingo] Error parsing message: {ex.Message}");
          }
        });

      client.Start().Wait();

      var subscribeMessage = new
      {
        eventName = "subscribe",
        authorization = tiingoToken,
        eventData = new
        {
          thresholdLevel = 5,
          tickers = new[] { "eurusd", "usdjpy" }
        }
      };

      client.Send(JsonSerializer.Serialize(subscribeMessage));
    }

    public static PriceUpdate? GetLatestPrice(string symbol)
    {
      LatestPrices.TryGetValue(symbol.ToUpper(), out var price);
      return price;
    }
  }
}