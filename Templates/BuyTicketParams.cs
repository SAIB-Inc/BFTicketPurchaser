using Chrysalis.Cbor.Types.Cardano.Core.Common;
using Chrysalis.Cbor.Types.Cardano.Core.Transaction;
using Chrysalis.Tx.Models;

namespace BFTicketPurchaser.Templates;

/// <summary>
/// Parameters required for a ticket purchase transaction.
/// </summary>
public record BuyTicketParams(
    /// <summary>
    /// The buyer's bech32 address (where ticket + change goes)
    /// </summary>
    string BuyerAddress,

    /// <summary>
    /// Current ticket counter from the state UTxO datum
    /// </summary>
    long TicketCounter,

    /// <summary>
    /// The state UTxO reference (TxHash#Index)
    /// </summary>
    TransactionInput StateUtxoRef,

    /// <summary>
    /// The value from the state UTxO (must be preserved in output)
    /// </summary>
    Value StateUtxoValue
) : ITransactionParameters
{
    /// <summary>
    /// Dynamic parties for the transaction.
    /// Buyer is set as change address so ticket + remaining ADA goes there.
    /// </summary>
    public Dictionary<string, (string address, bool isChange)> Parties { get; set; } = new()
    {
        { "buyer", (BuyerAddress, true) },
        { "change", (BuyerAddress, true) }
    };
}
