using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace backend.Hubs
{
  public class PriceHub : Hub
  {
    public async Task Subscribe(string symbol)
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, symbol.ToUpper());
      Console.WriteLine($"Client {Context.ConnectionId} subscribed to {symbol}");
    }

    public async Task Unsubscribe(string symbol)
    {
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpper());
      Console.WriteLine($"Client {Context.ConnectionId} unsubscribed from {symbol}");
    }
  }
}