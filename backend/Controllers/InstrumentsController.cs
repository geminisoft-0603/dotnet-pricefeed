using Microsoft.AspNetCore.Mvc;
using backend.Models;

[ApiController]
[Route("api/[controller]")]
public class InstrumentsController : ControllerBase
{
  private static readonly List<Instrument> instruments = new()
  {
    new Instrument { Symbol = "BTCUSDT" },
    new Instrument { Symbol = "EURUSD" },
    new Instrument { Symbol = "USDJPY" }
  };

  [HttpGet]
  public IActionResult GetInstruments() => Ok(instruments);

  [HttpGet("{symbol}")]
  public IActionResult GetPrice(string symbol)
  {
    var price = PriceService.GetLatestPrice(symbol.ToUpper());
    return price != null ? Ok(price) : NotFound("Price not available");
  }
}
