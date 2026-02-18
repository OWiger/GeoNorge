# GeoNorge Download Client

.NET library + console app for ordering and downloading datasets from GeoNorge Nedlasting API.

This repository was built and tested against:

- https://nedlasting.geonorge.no/help
- https://nedlasting.geonorge.no/help/documentation

## What this app does

- Calls GeoNorge capabilities/codelists/order APIs
- Supports restricted datasets requiring authentication
- Acquires bearer token from username/password automatically (GeoID)
- Caches bearer token with expiry
- Creates order and downloads ready files in one command

## Prerequisites

- .NET SDK (solution currently targets `net10.0`)
- GeoNorge user account with access to the target dataset

## Build

```powershell
dotnet build .\GeoNorge.DownloadClient.slnx
```

## Quick start (end-to-end)

Run the full flow (order + download) with default values:

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download
```

Run with interactive selection prompts:

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download --interactive true
```

## Interactive mode

Start interactive mode:

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download --interactive true
```

Interactive mode uses numbered list selection for:

- Data source (searched from Kartkatalog: https://kartkatalog.geonorge.no)
- Area
- Projection (coordinate system)
- Format
- Usage group
- Usage purpose

How to use it:

- Press `Enter` to keep the default value shown in brackets.
- Type a number to pick another option from the list.
- Data source search defaults to `FKB`; enter another search text to narrow to a different dataset family.

The selected data source sets the metadata UUID automatically for the rest of the flow.

If credentials are not already stored, the CLI prompts for:

- GeoNorge username
- GeoNorge password

Downloaded file is saved under:

- `./downloads`

## Default `order-download` settings

The command defaults to the tested FKB-Bygning setup:

- Metadata UUID: `8b4304ea-4fb0-479c-a24d-fa225e2c6e97` (FKB-Bygning)
- Area: `3901` (`Horten`, `kommune`)
- Projection: `5972` (`EUREF89 UTM sone 32, 2d + NN2000`)
- Format: `GML`
- Usage group: `næringsliv`
- Usage purpose: `tekoginnovasjon`

## Override defaults

Use arguments to target another dataset/area/format:

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download \
  --metadata-uuid "<uuid>" \
  --area-code "3201" \
  --area-name "Bærum" \
  --area-type "kommune" \
  --projection-code "5972" \
  --projection-name "EUREF89 UTM sone 32, 2d + NN2000" \
  --projection-codespace "http://www.opengis.net/def/crs/EPSG/0/5972" \
  --format-name "GML" \
  --usage-group "næringsliv" \
  --usage-purpose "tekoginnovasjon" \
  --output-dir ".\downloads"
```

## Authentication model

### Credential sources (priority)

1. `--username` and `--password`
2. `GEONORGE_USERNAME` and `GEONORGE_PASSWORD`
3. Stored credentials from first-run prompt

### Token sources (priority)

1. `--token`
2. `GEONORGE_BEARER_TOKEN`
3. Cached local token (if still valid)
4. Auto-acquire from username/password

### Local cache files

- Credentials: `%APPDATA%\GeoNorge.DownloadClient\credentials.json`
- Bearer token: `%APPDATA%\GeoNorge.DownloadClient\bearer-token.json`

On `401 Unauthorized`, cached token and stored credentials are cleared.

## Useful commands

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- help
dotnet run --project .\GeoNorge.DownloadClient.Cli -- capabilities <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- areas <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- projections <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- formats <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-get <referenceNumber>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- download-file <referenceNumber> <fileId> <destinationPath>
```

## Environment variable examples

```powershell
$env:GEONORGE_USERNAME="myuser"
$env:GEONORGE_PASSWORD="mypassword"
$env:GEONORGE_BASE_URL="https://nedlasting.geonorge.no"

dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download
```

Or use explicit bearer token:

```powershell
$env:GEONORGE_BEARER_TOKEN="<token>"
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download
```

## Troubleshooting

- **`Order contains restricted datasets, but no user information is provided`**
  - Ensure `usageGroup` and `usagePurpose` are set (defaults do this).
- **`401 Unauthorized`**
  - Re-run and re-enter credentials; cached auth is cleared on 401.
- **No files in order response**
  - Check area/projection/format combination against codelists for that dataset.

## Security note

- Treat bearer tokens like passwords.
- Rotate/revoke tokens if they have been shared in logs/chat.
