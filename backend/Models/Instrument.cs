namespace backend.Models
{
  public class Instrument
  {
    public string Symbol { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Binance or Tiingo
  }
}