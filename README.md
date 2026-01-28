# BuidlerFest Ticket Purchaser

A .NET CLI application for purchasing BuidlerFest 2026 tickets on Cardano mainnet using the [Chrysalis](https://github.com/SAIB-Inc/Chrysalis) framework.

## Overview

This tool builds and submits transactions to purchase BuidlerFest 2026 event tickets. It interacts with the on-chain ticket issuer contract, minting unique ticket NFTs (TICKET1, TICKET2, etc.) for each purchase.

## Features

- Queries the current ticket state UTxO via Kupo
- Builds unsigned transactions for ticket purchases
- Supports CIP30 wallet signing workflow
- Merges wallet witnesses and submits transactions via Ogmios

## Requirements

- .NET 10.0+
- Chrysalis 1.0.5-alpha
- Access to Kupo and Ogmios endpoints

## Usage

### Build an unsigned transaction

```bash
dotnet run <buyer_address>
```

Example:
```bash
dotnet run addr1qy...
```

This outputs the unsigned transaction CBOR for signing with a CIP30 wallet.

### Finalize and submit

After signing with your wallet (e.g., via `signTx`), merge the witness and submit:

```bash
dotnet run finalize <tx_cbor> <witness_cbor> --submit
```

## Configuration

Edit `Config/BuidlerFestConfig.cs` to modify:

- Kupo/Ogmios endpoints
- Policy IDs and addresses
- Ticket pricing

## Protocol

The BuidlerFest ticket protocol uses:

- **Beacon token**: Identifies the state UTxO containing the ticket counter
- **State datum**: Tracks the current ticket number
- **Ticket NFTs**: Minted with names like `TICKET52`, `TICKET53`, etc.
- **Treasury payment**: 400 ADA (early bird) or 500 ADA (after Feb 1, 2026)

## License

MIT
