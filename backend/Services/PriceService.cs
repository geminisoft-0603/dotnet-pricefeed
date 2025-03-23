using System.Collections.Concurrent;
using Websocket.Client;
using System.Reactive.Linq;
using System.Text.Json;
using backend.Models;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using System.Text;
using System.Net.Http.Headers;

namespace backend.Services
{
  public static class PriceService
  {
    private static readonly ConcurrentDictionary<string, PriceUpdate> LatestPrices = new();
    private static IHubContext<PriceHub>? _hubContext;

    public static async Task Start(IHubContext<PriceHub> hubContext)
    {
      _hubContext = hubContext;

      var tiingoToken = Environment.GetEnvironmentVariable("TIINGO_API_KEY") ?? string.Empty;

      if (!string.IsNullOrWhiteSpace(tiingoToken))
      {
        await LoadInitialTiingoPrices(tiingoToken);
        StartTiingo(tiingoToken);
      }
      else
      {
        Console.WriteLine("[Tiingo] No API token provided. Skipping Tiingo setup.");
      }

      StartBinance();
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
          }
          catch (Exception ex)
          {
            Console.WriteLine($"[Binance] Error: {ex.Message}");
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

    private static void StartTiingo(string tiingoToken)
    {
      var url = new Uri("wss://api.tiingo.com/fx");
      var client = new WebsocketClient(url)
      {
        IsReconnectionEnabled = true
      };

      var subscribeMessage = new
      {
        eventName = "subscribe",
        authorization = tiingoToken,
        eventData = new
        {
          thresholdLevel = "5",
          tickers = new[] { "eurusd", "usdjpy", "gbpusd" }
        }
      };

      client.ReconnectionHappened.Subscribe(info =>
      {
        Console.WriteLine($"[Tiingo] Reconnected: {info.Type}");
        client.Send(JsonSerializer.Serialize(subscribeMessage));
      });

      client.DisconnectionHappened.Subscribe(info =>
      {
        Console.WriteLine($"[Tiingo] Disconnected: {info.Type} - {info.Exception?.Message}");
      });

      client.MessageReceived
        .Where(msg => msg.Text != null)
        .Subscribe(async msg =>
        {
          Console.WriteLine($"[Tiingo] Raw message: {msg.Text}");

          try
          {
            if (_hubContext == null) return;

            var json = JsonDocument.Parse(msg.Text!);
            var root = json.RootElement;

            if (root.TryGetProperty("messageType", out var typeElement) &&
                typeElement.GetString() == "A" &&
                root.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind == JsonValueKind.Array &&
                dataElement.GetArrayLength() >= 8 &&
                dataElement[0].GetString() == "Q")
            {
              var symbol = dataElement[1].GetString()?.ToUpper();
              var midPrice = dataElement[5].GetDecimal();
              var timestamp = DateTime.UtcNow;

              if (DateTime.TryParse(dataElement[2].GetString(), out var parsedTime))
              {
                timestamp = parsedTime.ToUniversalTime();
              }

              if (!string.IsNullOrEmpty(symbol))
              {
                var update = new PriceUpdate
                {
                  Symbol = symbol,
                  Price = midPrice,
                  Timestamp = timestamp
                };

                LatestPrices[symbol] = update;
                await _hubContext.Clients.Group(symbol).SendAsync("ReceivePrice", update);
                Console.WriteLine($"[Tiingo] {symbol} = {midPrice} @ {timestamp:O}");
              }
            }
            else if (typeElement.GetString() == "H")
            {
              Console.WriteLine("[Tiingo] Heartbeat received.");
            }
            else
            {
              Console.WriteLine($"[Tiingo] Ignored message: {msg.Text}");
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine($"[Tiingo] Error parsing message: {ex.Message}");
          }
        });

      client.Start().Wait();
      client.Send(JsonSerializer.Serialize(subscribeMessage));
    }

    public static async Task LoadInitialTiingoPrices(string token)
    {
      try
      {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

        var url = "https://api.tiingo.com/tiingo/fx/top?tickers=eurusd,usdjpy,gbpusd";
        var response = await client.GetStringAsync(url);
        var json = JsonDocument.Parse(response);

        foreach (var item in json.RootElement.EnumerateArray())
        {
          var symbol = item.GetProperty("ticker").GetString()?.ToUpper();
          var midPrice = item.GetProperty("midPrice").GetDecimal();
          var timestamp = item.GetProperty("quoteTimestamp").GetDateTime();

          if (!string.IsNullOrEmpty(symbol))
          {
            var update = new PriceUpdate
            {
              Symbol = symbol,
              Price = midPrice,
              Timestamp = timestamp.ToUniversalTime()
            };

            LatestPrices[symbol] = update;
            Console.WriteLine($"[Tiingo REST] Loaded {symbol} = {midPrice} @ {timestamp:O}");
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Tiingo REST] Error: {ex.Message}");
      }
    }

    public static PriceUpdate? GetLatestPrice(string symbol)
    {
      LatestPrices.TryGetValue(symbol.ToUpper(), out var price);
      return price;
    }
  }
}
