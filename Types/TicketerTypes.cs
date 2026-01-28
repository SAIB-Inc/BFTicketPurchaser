using Chrysalis.Cbor.Serialization.Attributes;
using Chrysalis.Cbor.Types;

namespace BFTicketPurchaser.Types;

/// <summary>
/// Datum stored at the Issuer's state UTxO.
/// Tracks the current ticket counter for sequential NFT naming.
/// On-chain uses definite encoding: d87981{counter}
/// </summary>
[CborSerializable]
[CborConstr(0)]
public partial record TicketerDatum(
    [CborOrder(0)] long TicketCounter
) : CborBase;

/// <summary>
/// Redeemer for spending the Issuer's state UTxO.
/// BuyTicket is the only action (Constr 0, no fields).
/// CBOR: d87980 (Constr 0, definite empty array)
/// </summary>
[CborSerializable]
[CborConstr(0)]
[CborDefinite]
public partial record BuyTicketRedeemer() : CborBase;

/// <summary>
/// Redeemer for minting ticket tokens.
/// </summary>
[CborSerializable]
[CborUnion]
public abstract partial record TicketPolicyRedeemer : CborBase;

/// <summary>
/// MintTicket - Constr 0, used when purchasing a ticket.
/// CBOR: d87980 (Constr 0, definite empty array)
/// </summary>
[CborSerializable]
[CborConstr(0)]
[CborDefinite]
public partial record MintTicket() : TicketPolicyRedeemer;

/// <summary>
/// BurnTicket - Constr 1, used when burning tickets.
/// CBOR: d87a80 (Constr 1, definite empty array)
/// </summary>
[CborSerializable]
[CborConstr(1)]
[CborDefinite]
public partial record BurnTicket() : TicketPolicyRedeemer;
