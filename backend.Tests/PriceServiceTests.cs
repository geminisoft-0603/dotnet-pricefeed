using Xunit;
using backend.Services;
using backend.Models;
using System.Reflection;
using System.Collections.Concurrent;

namespace backend.Tests;

public class PriceServiceTests
{
    [Fact]
    public void GetLatestPrice_ReturnsExpectedPrice()
    {
      // Arrange
      var symbol = "BTCUSDT";
      var expected = new PriceUpdate
      {
          Symbol = symbol,
          Price = 12345.67m,
          Timestamp = DateTime.UtcNow
      };

      var dictField = typeof(PriceService).GetField("LatestPrices", BindingFlags.NonPublic | BindingFlags.Static);
      var dict = (ConcurrentDictionary<string, PriceUpdate>)dictField!.GetValue(null)!;
      dict[symbol] = expected;

      // Act
      var result = PriceService.GetLatestPrice(symbol);

      // Assert
      Assert.NotNull(result);
      Assert.Equal(expected.Price, result!.Price);
    }
}
