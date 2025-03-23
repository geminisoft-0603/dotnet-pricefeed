using Microsoft.AspNetCore.SingalR;
using backend.Models;

public class PriceHub : Hub
{
  public async Task Subscribe(string symbol)
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, symbol.ToUpper());
    Console.WriteLine($"Client {Context.ConnectionId} subscribed to {symbol}");
  }

  public static async Task BroadcastPrice(IHubContext<PriceHub> hub, PriceUpdate price)
  {
    await hub.Clients.Group(price.Symbol.ToUpper()).SendAsync("ReceivePrice", price);
  }
}