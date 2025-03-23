using Xunit;
using backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;
using System.Collections.Generic;

namespace backend.Tests;

public class InstrumentsControllerTests
{
  private readonly InstrumentsController _controller = new();

  [Fact]
  public void GetInstruments_ReturnsList()
  {
    var result = _controller.GetInstruments();
    var okResult = Assert.IsType<OkObjectResult>(result);
    var list = Assert.IsAssignableFrom<IEnumerable<Instrument>>(okResult.Value);
    Assert.Contains(list, i => i.Symbol == "BTCUSDT");
  }

  [Fact]
  public void GetPrice_NotFound()
  {
    var result = _controller.GetPrice("INVALID");
    Assert.IsType<NotFoundObjectResult>(result);
  }
}
