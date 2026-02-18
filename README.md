# GeoNorge Download Client

Simple .NET client + CLI for GeoNorge Nedlasting API, based on:

- https://github.com/kartverket/Geonorge.NedlastingKlient
- https://nedlasting.geonorge.no/help/documentation

## Build

```powershell
dotnet build .\GeoNorge.DownloadClient.slnx
```

## CLI usage

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- capabilities <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- areas <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- projections <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- formats <metadataUuid>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- can-download <metadataUuid> <coordinateSystem> <coordinates>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- auth-test
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-create <orderRequest.json>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-get <referenceNumber>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- download-file <referenceNumber> <fileId> <destinationPath>
```

`auth-test` verifies Basic auth by calling `https://httpbin.org/basic-auth/{username}/{password}` with your configured credentials.

For access-restricted datasets, provide credentials (Basic auth):

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- --username <username> --password <password> order-get <referenceNumber>
dotnet run --project .\GeoNorge.DownloadClient.Cli -- --username <username> --password <password> download-file <referenceNumber> <fileId> <destinationPath>
```

If username/password are not provided, the CLI prompts on first run for protected commands (`order-create`, `order-get`, `download-file`) and stores them for later runs.
If authentication fails with `401`, saved credentials are cleared and you will be prompted again on the next run.

## `order-download` defaults and overrides

`order-download` runs the complete flow (create order + download files) with defaults matching your latest working setup:

- Metadata: `8b4304ea-4fb0-479c-a24d-fa225e2c6e97` (`FKB-Bygning`)
- Area: `3901` / `Horten` / `kommune`
- Projection: `5972` (`EUREF89 UTM sone 32, 2d + NN2000`)
- Format: `GML`
- Usage: `næringsliv` + `tekoginnovasjon`
- Output directory: `./downloads`

`order-download` can acquire bearer token automatically from username/password.

Credential sources (priority):

1. `--username` / `--password`
2. `GEONORGE_USERNAME` / `GEONORGE_PASSWORD`
3. Stored credentials from first-run prompt

Token sources (priority):

1. `--token`
2. `GEONORGE_BEARER_TOKEN`
3. Auto-acquire from credentials above (GeoID token endpoint)

Auto-acquired token is cached locally with expiry in `%APPDATA%\\GeoNorge.DownloadClient\\bearer-token.json` and reused until near expiration.
If API returns `401`, the cached token is cleared automatically.

Example:

```powershell
$env:GEONORGE_USERNAME="myuser"
$env:GEONORGE_PASSWORD="mypassword"
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download
```

Override example:

```powershell
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-download --token "<token>" --metadata-uuid "<uuid>" --area-code "3201" --area-name "Bærum" --area-type "kommune" --projection-code "5972" --projection-name "EUREF89 UTM sone 32, 2d + NN2000" --projection-codespace "http://www.opengis.net/def/crs/EPSG/0/5972" --format-name "GML" --usage-group "næringsliv" --usage-purpose "tekoginnovasjon" --output-dir ".\downloads"
```

You can also use environment variables:

```powershell
$env:GEONORGE_USERNAME="myuser"
$env:GEONORGE_PASSWORD="mypassword"
$env:GEONORGE_BASE_URL="https://nedlasting.geonorge.no"
dotnet run --project .\GeoNorge.DownloadClient.Cli -- order-get <referenceNumber>
```

## `orderRequest.json` example

```json
{
  "email": "user@example.com",
  "orderLines": [
    {
      "metadataUuid": "73f863ba-628f-48af-b7fa-30d3ab331b8d",
      "areas": [
        { "code": "02", "type": "fylke", "name": "Akershus" }
      ],
      "projections": [
        {
          "code": "25832",
          "name": "EUREF89 UTM sone 32, 2d",
          "codespace": "http://www.opengis.net/def/crs/EPSG/0/25832"
        }
      ],
      "formats": [
        { "name": "SOSI 4.5" }
      ]
    }
  ]
}
```

For polygon orders, set `areas` to polygon and include `coordinates` as documented by GeoNorge.
