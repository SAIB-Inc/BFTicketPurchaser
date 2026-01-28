using BFTicketPurchaser.Config;
using BFTicketPurchaser.Types;
using Chrysalis.Cbor.Types.Cardano.Core.Common;
using Chrysalis.Cbor.Types.Cardano.Core.Transaction;
using Chrysalis.Tx.Builders;
using Chrysalis.Tx.Models;
using Chrysalis.Tx.Utils;

namespace BFTicketPurchaser.Templates;

public static class BuyTicketTemplate
{
    /// <summary>
    /// Creates a transaction template for purchasing a BuidlerFest ticket.
    /// </summary>
    public static TransactionTemplate<BuyTicketParams> Create(ICardanoDataProvider provider)
    {
        return TransactionTemplateBuilder<BuyTicketParams>.Create(provider)
            // Define static parties (buyer comes from ITransactionParameters.Parties)
            .AddStaticParty("issuer", BuidlerFestConfig.IssuerAddress)
            .AddStaticParty("treasury", BuidlerFestConfig.TreasuryAddress)
            .AddStaticParty("scriptRefHolder", BuidlerFestConfig.ScriptRefAddress)

            // Reference input: Script reference UTxO (contains the validator)
            .AddReferenceInput((options, _) =>
            {
                options.From = "scriptRefHolder";
                options.Id = "scriptRef";
                options.UtxoRef = new TransactionInput(
                    Convert.FromHexString(BuidlerFestConfig.ScriptRefTxHash),
                    BuidlerFestConfig.ScriptRefIndex
                );
            })

            // Input: State UTxO (contains beacon token + counter datum)
            .AddInput((options, param) =>
            {
                options.From = "issuer";
                options.Id = "stateUtxo";
                // Use the exact UTxO reference from params
                options.UtxoRef = param.StateUtxoRef;
                // Spend redeemer: BuyTicket (unit)
                options.SetRedeemerBuilder<BuyTicketRedeemer>((mapping, p, builder) => new BuyTicketRedeemer());
            })

            // Mint: Ticket NFT
            .AddMint((options, param) =>
            {
                options.Policy = BuidlerFestConfig.TicketPolicy;
                // Asset name is "TICKET" + counter (e.g., "TICKET52")
                string ticketName = $"TICKET{param.TicketCounter}";
                string ticketNameHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(ticketName));
                options.Assets = new Dictionary<string, long>
                {
                    { ticketNameHex, 1 }
                };
                // Mint redeemer: MintTicket (unit)
                options.SetRedeemerBuilder<MintTicket>((mapping, p, builder) => new MintTicket());
            })

            // Output 1: Updated state back to issuer
            .AddOutput((options, param, fee) =>
            {
                options.To = "issuer";
                options.Id = "newState";
                options.AssociatedInputId = "stateUtxo";
                // Preserve the exact value from the input UTxO (on-chain requirement)
                options.Amount = param.StateUtxoValue;
                // Updated datum with incremented counter
                options.SetDatum(new TicketerDatum(param.TicketCounter + 1));
            })

            // Output 2: Payment to treasury
            .AddOutput((options, param, fee) =>
            {
                options.To = "treasury";
                options.Id = "payment";
                options.Amount = new Lovelace(BuidlerFestConfig.GetCurrentPrice());
            })

            // Validity window (~5 minutes)
            .SetValidFrom(GetCurrentSlot())
            .SetValidTo(GetCurrentSlot() + 300)

            .Build();
    }

    /// <summary>
    /// Gets the current slot number using Chrysalis SlotUtil.
    /// </summary>
    private static ulong GetCurrentSlot()
    {
        var slotConfig = SlotUtil.GetSlotNetworkConfig(BuidlerFestConfig.Network);
        var currentSlot = SlotUtil.GetSlotFromUTCTime(slotConfig, DateTime.UtcNow);
        return (ulong)currentSlot;
    }
}
