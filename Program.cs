using BFTicketPurchaser.Config;
using BFTicketPurchaser.Templates;
using BFTicketPurchaser.Types;
using Chrysalis.Cbor.Extensions.Cardano.Core.Common;
using Chrysalis.Cbor.Extensions.Cardano.Core.Transaction;
using Chrysalis.Cbor.Extensions.Cardano.Core.TransactionWitness;
using Chrysalis.Cbor.Serialization;
using Chrysalis.Cbor.Types.Cardano.Core.Common;
using Chrysalis.Cbor.Types.Cardano.Core.Transaction;
using Chrysalis.Cbor.Types.Cardano.Core.TransactionWitness;
using Chrysalis.Tx.Extensions;
using Chrysalis.Tx.Providers;
using CborTransaction = Chrysalis.Cbor.Types.Cardano.Core.Transaction.Transaction;

// Parse CLI arguments
if (args.Length == 0)
{
    Console.WriteLine("=== BuidlerFest Ticket Purchaser ===\n");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run <buyer_address>                        - Build unsigned transaction");
    Console.WriteLine("  dotnet run finalize <tx_cbor> <witness> [--submit] - Merge witness into tx");
    Console.WriteLine("");
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run addr1qy...");
    Console.WriteLine("  dotnet run finalize 84aa00... a10081... --submit");
    Console.WriteLine("");
    Console.WriteLine("This will build an unsigned transaction to purchase a ticket.");
    Console.WriteLine("The transaction CBOR will be output for signing with your wallet.");
    return;
}

// Handle finalize command
if (args[0].ToLower() == "finalize")
{
    if (args.Length < 3)
    {
        Console.WriteLine("ERROR: finalize requires <tx_cbor> and <witness> arguments");
        return;
    }

    var txCborHex = args[1];
    var witnessCborHex = args[2];
    var shouldSubmit = args.Length > 3 && args[3] == "--submit";

    Console.WriteLine("=== Finalizing Transaction ===\n");

    try
    {
        // Deserialize the unsigned transaction
        var unsignedTx = CborSerializer.Deserialize<CborTransaction>(Convert.FromHexString(txCborHex));

        // Deserialize the witness set from CIP30
        var witnessSet = CborSerializer.Deserialize<TransactionWitnessSet>(Convert.FromHexString(witnessCborHex));
        var vKeyWitnesses = witnessSet.VKeyWitnessSet()?.ToList() ?? [];

        Console.WriteLine($"Found {vKeyWitnesses.Count} VKey witness(es)");

        // Sign (merge witness into tx)
        var signedTx = unsignedTx.Sign(vKeyWitnesses);
        var signedCbor = CborSerializer.Serialize(signedTx);
        var signedCborHex = Convert.ToHexString(signedCbor).ToLower();

        Console.WriteLine("\nSigned Transaction CBOR:");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine(signedCborHex);
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"\nLength: {signedCborHex.Length / 2} bytes");

        if (shouldSubmit)
        {
            Console.WriteLine("\nSubmitting transaction...");
            var submitProvider = new Kupmios(
                BuidlerFestConfig.KupoEndpoint,
                BuidlerFestConfig.OgmiosEndpoint,
                BuidlerFestConfig.Network
            );
            var txHash = await submitProvider.SubmitTransactionAsync(signedTx);
            Console.WriteLine($"Transaction submitted! TxHash: {txHash}");
        }
        else
        {
            Console.WriteLine("\nUse --submit flag to submit, or submit manually.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.WriteLine($"Details: {ex}");
    }

    return;
}

var buyerAddress = args[0];

// Validate address format
if (!buyerAddress.StartsWith("addr1"))
{
    Console.WriteLine("ERROR: Invalid address format. Must be a mainnet address starting with 'addr1'");
    return;
}

Console.WriteLine("=== BuidlerFest Ticket Purchaser ===\n");
Console.WriteLine($"Buyer Address: {buyerAddress}\n");

// 1. Create the Kupmios provider
Console.WriteLine("1. Connecting to Kupo/Ogmios...");
var provider = new Kupmios(
    BuidlerFestConfig.KupoEndpoint,
    BuidlerFestConfig.OgmiosEndpoint,
    BuidlerFestConfig.Network
);
Console.WriteLine($"   Kupo:   {BuidlerFestConfig.KupoEndpoint}");
Console.WriteLine($"   Ogmios: {BuidlerFestConfig.OgmiosEndpoint}");
Console.WriteLine($"   Network: {BuidlerFestConfig.Network}");

// 2. Query the current state UTxO by subject (beacon token)
Console.WriteLine("\n2. Querying state UTxO...");
var beaconSubject = $"{BuidlerFestConfig.IssuerBeaconPolicy}.{BuidlerFestConfig.IssuerBeaconName}";

var beaconUtxos = await provider.GetUtxosAsync([beaconSubject]);
Console.WriteLine($"   Found {beaconUtxos.Count} UTxO(s) with beacon token");

// Filter to find the one at the issuer/validator address
var issuerAddressBytes = Chrysalis.Wallet.Models.Addresses.Address.FromBech32(BuidlerFestConfig.IssuerAddress).ToBytes();
var stateUtxo = beaconUtxos.FirstOrDefault(utxo =>
{
    var outputAddress = utxo.Output switch
    {
        PostAlonzoTransactionOutput post => post.Address?.Value,
        AlonzoTransactionOutput alonzo => alonzo.Address?.Value,
        _ => null
    };
    return outputAddress != null && outputAddress.SequenceEqual(issuerAddressBytes);
});

if (stateUtxo == null)
{
    Console.WriteLine("   ERROR: Could not find state UTxO at issuer address!");
    return;
}

Console.WriteLine($"   State UTxO: {Convert.ToHexString(stateUtxo.Outref.TransactionId).ToLower()}#{stateUtxo.Outref.Index}");

// Also fetch the script reference UTxO
Console.WriteLine("\n   Fetching script reference UTxO...");
var scriptRefUtxo = await provider.GetUtxoByOutRefAsync(
    BuidlerFestConfig.ScriptRefTxHash,
    BuidlerFestConfig.ScriptRefIndex
);

if (scriptRefUtxo == null)
{
    Console.WriteLine("   ERROR: Could not find script reference UTxO!");
    return;
}

var hasScript = scriptRefUtxo.Output is PostAlonzoTransactionOutput postOut && postOut.ScriptRef != null;
Console.WriteLine($"   Script Ref UTxO: {BuidlerFestConfig.ScriptRefTxHash}#{BuidlerFestConfig.ScriptRefIndex}");
Console.WriteLine($"   Has script: {hasScript}");

// Check what address the script ref is at
var scriptRefAddress = scriptRefUtxo.Output switch
{
    PostAlonzoTransactionOutput post => post.Address?.Value,
    AlonzoTransactionOutput alonzo => alonzo.Address?.Value,
    _ => null
};
if (scriptRefAddress != null)
{
    var scriptRefBech32 = Chrysalis.Wallet.Models.Addresses.Address.FromBytes(scriptRefAddress).ToBech32();
    Console.WriteLine($"   Script Ref Address: {scriptRefBech32}");
    Console.WriteLine($"   Issuer Address:     {BuidlerFestConfig.IssuerAddress}");
    Console.WriteLine($"   Same address: {scriptRefBech32 == BuidlerFestConfig.IssuerAddress}");
}

// 3. Parse the datum to get the current counter
var datumBytes = stateUtxo.Output switch
{
    PostAlonzoTransactionOutput post when post.Datum is InlineDatumOption inline
        => inline.Data.Value,
    _ => null
};

if (datumBytes == null)
{
    Console.WriteLine("   ERROR: State UTxO has no inline datum!");
    return;
}

var currentDatum = CborSerializer.Deserialize<TicketerDatum>(datumBytes);
Console.WriteLine($"   Current counter: {currentDatum.TicketCounter}");
Console.WriteLine($"   Your ticket: TICKET{currentDatum.TicketCounter}");

// 4. Show state UTxO value (must be preserved)
var stateValue = stateUtxo.Output.Amount();
var stateLovelace = stateValue.Lovelace();
Console.WriteLine($"\n3. State UTxO Value:");
Console.WriteLine($"   Lovelace: {stateLovelace} ({stateLovelace / 1_000_000m} ADA)");
if (stateValue is LovelaceWithMultiAsset multiAsset)
{
    foreach (var (policyId, bundle) in multiAsset.MultiAsset.Value)
    {
        foreach (var (assetName, qty) in bundle.Value)
        {
            Console.WriteLine($"   Token: {Convert.ToHexString(policyId).ToLower()}.{Convert.ToHexString(assetName).ToLower()} x {qty}");
        }
    }
}

// 5. Get ticket price
var price = BuidlerFestConfig.GetCurrentPrice();
Console.WriteLine($"\n4. Ticket price: {price / 1_000_000} ADA");

// 6. Build the transaction
Console.WriteLine("\n5. Building transaction...");

var buyTicketTemplate = BuyTicketTemplate.Create(provider);

var txParams = new BuyTicketParams(
    BuyerAddress: buyerAddress,
    TicketCounter: currentDatum.TicketCounter,
    StateUtxoRef: stateUtxo.Outref,
    StateUtxoValue: stateUtxo.Output.Amount()
);

try
{
    var unsignedTx = await buyTicketTemplate(txParams);
    var txCbor = CborSerializer.Serialize(unsignedTx);
    var txCborHex = Convert.ToHexString(txCbor).ToLower();

    Console.WriteLine("   Transaction built successfully!");
    Console.WriteLine($"\n6. Unsigned Transaction CBOR:");
    Console.WriteLine("   ----------------------------------------");
    Console.WriteLine($"   {txCborHex}");
    Console.WriteLine("   ----------------------------------------");
    Console.WriteLine($"\n   Length: {txCborHex.Length / 2} bytes");
    Console.WriteLine("\n7. Next steps:");
    Console.WriteLine("   1. Sign this transaction with your wallet");
    Console.WriteLine("   2. Submit the signed transaction to the network");
    Console.WriteLine("\n=== Transaction Ready for Signing ===");
}
catch (Exception ex)
{
    Console.WriteLine($"   ERROR: Failed to build transaction: {ex.Message}");
    Console.WriteLine($"\n   Details: {ex}");
}
