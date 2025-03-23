using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class InstrumentsController : ControllerBase
  {
    private static readonly List<Instrument> Instruments = new()
    {
      new Instrument { Symbol = "BTCUSDT", Source = "Binance" },
      new Instrument { Symbol = "EURUSD", Source = "Tiingo" },
      new Instrument { Symbol = "USDJPY", Source = "Tiingo" }
    };

    [HttpGet]
    public IActionResult GetInstruments()
    {
      try
      {
        return Ok(Instruments);
      }
      catch (Exception ex)
      {
        return StatusCode(500, $"Error: {ex.Message}");
      }
    }

    [HttpGet("{symbol}")]
    public IActionResult GetPrice(string symbol)
    {
      try
      {
        var price = PriceService.GetLatestPrice(symbol);
        return price != null ? Ok(price) : NotFound("Price not available");
      }
      catch (Exception ex)
      {
        return StatusCode(500, $"Error retrieving price: {ex.Message}");
      }
    }
  }
}