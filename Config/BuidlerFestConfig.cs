using Chrysalis.Wallet.Models.Enums;

namespace BFTicketPurchaser.Config;

public static class BuidlerFestConfig
{
    // Network
    public const NetworkType Network = NetworkType.Mainnet;

    // Kupo & Ogmios endpoints (replace with your own)
    public const string KupoEndpoint = "https://your-kupo-endpoint/";
    public const string OgmiosEndpoint = "https://your-ogmios-endpoint/";

    // Policy IDs
    public const string IssuerBeaconPolicy = "e1ddde8138579e255482791d9fba0778cb1f5c7b435be7b3e42069de";
    public const string IssuerBeaconName = "425549444c45524645535432303236"; // "BUIDLERFEST2026" in hex
    public const string TicketPolicy = "1d9c0b541adc300c19ddc6b9fb63c0bfe32b1508305ba65b8762dc7b";

    // Addresses
    public const string TreasuryAddress = "addr1qx0decp93g2kwym5cz0p68thamd2t9pehlxqe02qae5r6nycv42qmjppm2rr8fj6qlzfhm6ljkd5f0tjlgudtmt5kzyqmy8x82";
    public const string IssuerAddress = "addr1wywecz65rtwrqrqemhrtn7mrczl7x2c4pqc9hfjmsa3dc7cr5pvqw";
    public const string ScriptRefAddress = "addr1wy8ccvgzslpjf9yhrprvmqulpmjpkpxf8c0hvtjwvw8n6pqdcrnp0";  // Address holding the script reference UTxO

    // Script Reference UTxO
    public const string ScriptRefTxHash = "31596ecbdcf102c8e5c17e75c65cf9780996285879d18903f035964f3a7499a8";
    public const ulong ScriptRefIndex = 0;

    // Ticket price (in lovelace)
    public const ulong EarlyBirdPrice = 400_000_000;  // 400 ADA (before Feb 1, 2026)
    public const ulong NormalPrice = 500_000_000;     // 500 ADA (after Feb 1, 2026)

    // Get current ticket price based on date
    public static ulong GetCurrentPrice()
    {
        var switchDate = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        return DateTime.UtcNow < switchDate ? EarlyBirdPrice : NormalPrice;
    }
}
