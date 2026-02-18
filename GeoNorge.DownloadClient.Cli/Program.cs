using System.Net;
using System.Text;
using System.Text.Json;
using GeoNorge.DownloadClient;
using GeoNorge.DownloadClient.Cli;

var credentialStore = new CredentialStore();
var bearerTokenStore = new BearerTokenStore();
var parsed = ParseCliArgs(args);
var commandArgs = parsed.CommandArgs;

if (commandArgs.Length == 0)
{
	PrintUsage();
	return;
}

string command = commandArgs[0].ToLowerInvariant();
bool authRelevantCommand = IsAuthRelevantCommand(command);

if (authRelevantCommand)
{
	parsed = EnsureCredentials(parsed, credentialStore);
}

try
{
	var client = new GeoNorgeDownloadClient(
		baseUrl: parsed.BaseUrl,
		username: parsed.Username,
		password: parsed.Password);

	switch (command)
	{
			case "auth-test":
				string authTestUrl = commandArgs.Length >= 2
					? commandArgs[1]
					: BuildDefaultAuthTestUrl(parsed.Username, parsed.Password);

				await RunAuthTestAsync(authTestUrl, parsed.Username, parsed.Password);
				break;

			case "order-download":
				var defaults = OrderDownloadOptions.Default(parsed.BaseUrl);
				var options = ParseOrderDownloadOptions(commandArgs.Skip(1).ToArray(), defaults);
				await RunOrderDownloadAsync(client, options, parsed.Username, parsed.Password, bearerTokenStore);
				break;

		case "capabilities":
			RequireArgs(commandArgs, 2, "capabilities <metadataUuid>");
			var capabilities = await client.GetCapabilitiesAsync(commandArgs[1]);
			PrintJson(capabilities);
			break;

		case "areas":
			RequireArgs(commandArgs, 2, "areas <metadataUuid>");
			var areas = await client.GetAreasAsync(commandArgs[1]);
			PrintJson(areas);
			break;

		case "projections":
			RequireArgs(commandArgs, 2, "projections <metadataUuid>");
			var projections = await client.GetProjectionsAsync(commandArgs[1]);
			PrintJson(projections);
			break;

		case "formats":
			RequireArgs(commandArgs, 2, "formats <metadataUuid>");
			var formats = await client.GetFormatsAsync(commandArgs[1]);
			PrintJson(formats);
			break;

		case "can-download":
			RequireArgs(commandArgs, 4, "can-download <metadataUuid> <coordinateSystem> <coordinates>");
			var canDownload = await client.CanDownloadAsync(new CanDownloadRequest
			{
				MetadataUuid = commandArgs[1],
				CoordinateSystem = commandArgs[2],
				Coordinates = commandArgs[3]
			});
			PrintJson(canDownload);
			break;

		case "order-create":
			RequireArgs(commandArgs, 2, "order-create <orderRequest.json>");
			string jsonPath = commandArgs[1];
			if (!File.Exists(jsonPath))
			{
				throw new FileNotFoundException("Could not find order request json file.", jsonPath);
			}

			var json = await File.ReadAllTextAsync(jsonPath);
			var orderRequest = JsonSerializer.Deserialize<OrderRequest>(json, JsonOptions());
			if (orderRequest is null)
			{
				throw new InvalidOperationException("Could not parse order request JSON.");
			}

			var createdOrder = await client.CreateOrderAsync(orderRequest);
			PrintJson(createdOrder);
			break;

		case "order-get":
			RequireArgs(commandArgs, 2, "order-get <referenceNumber>");
			var order = await client.GetOrderAsync(commandArgs[1]);
			PrintJson(order);
			break;

		case "download-file":
			RequireArgs(commandArgs, 4, "download-file <referenceNumber> <fileId> <destinationPath>");
			await client.DownloadOrderFileAsync(commandArgs[1], commandArgs[2], commandArgs[3]);
			Console.WriteLine($"Downloaded to: {Path.GetFullPath(commandArgs[3])}");
			break;

		case "help":
		case "--help":
		case "-h":
			PrintUsage();
			break;

		default:
			throw new ArgumentException($"Unknown command: {command}");
	}
}
catch (GeoNorgeApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized && authRelevantCommand)
{
	credentialStore.Clear();
	bearerTokenStore.Clear();
	Console.Error.WriteLine("Authentication failed (401). Saved credentials were cleared.");
	Console.Error.WriteLine("Cached bearer token was cleared.");
	Console.Error.WriteLine("Run the command again to be prompted for username and password.");
	Environment.ExitCode = 1;
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
	Environment.ExitCode = 1;
}

static ParsedArgs EnsureCredentials(ParsedArgs parsed, CredentialStore credentialStore)
{
	if (!string.IsNullOrWhiteSpace(parsed.Username) && parsed.Password is not null)
	{
		return parsed;
	}

	StoredCredentials? stored = credentialStore.Load();
	if (stored is not null && !string.IsNullOrWhiteSpace(stored.Username) && !string.IsNullOrWhiteSpace(stored.Password))
	{
		return parsed with { Username = stored.Username, Password = stored.Password };
	}

	if (Console.IsInputRedirected)
	{
		throw new InvalidOperationException("Credentials are required but interactive prompt is not available. Set --username/--password or GEONORGE_USERNAME/GEONORGE_PASSWORD.");
	}

	Console.Write("GeoNorge username: ");
	string? username = Console.ReadLine()?.Trim();
	if (string.IsNullOrWhiteSpace(username))
	{
		throw new ArgumentException("Username is required.");
	}

	Console.Write("GeoNorge password: ");
	string password = ReadPassword();
	if (string.IsNullOrWhiteSpace(password))
	{
		throw new ArgumentException("Password is required.");
	}

	Console.WriteLine();
	credentialStore.Save(new StoredCredentials
	{
		Username = username,
		Password = password
	});

	return parsed with { Username = username, Password = password };
}

static bool IsAuthRelevantCommand(string command)
{
	return command is "order-get" or "order-create" or "download-file" or "auth-test" or "order-download";
}

static void PrintJson<T>(T value)
{
	string output = JsonSerializer.Serialize(value, JsonOptions());
	Console.WriteLine(output);
}

static JsonSerializerOptions JsonOptions() => new()
{
	PropertyNameCaseInsensitive = true,
	WriteIndented = true
};

static void RequireArgs(string[] args, int expectedCount, string usage)
{
	if (args.Length < expectedCount)
	{
		throw new ArgumentException($"Expected: {usage}");
	}
}

static void PrintUsage()
{
	Console.WriteLine("GeoNorge Download CLI");
	Console.WriteLine();
	Console.WriteLine("Options (global):");
	Console.WriteLine("  --username <value>");
	Console.WriteLine("  --password <value>");
	Console.WriteLine("  --base-url <url> (default: https://nedlasting.geonorge.no)");
	Console.WriteLine("  Env fallback: GEONORGE_USERNAME, GEONORGE_PASSWORD, GEONORGE_BASE_URL");
	Console.WriteLine("  If credentials are missing for protected commands, you will be prompted on first run.");
	Console.WriteLine();
	Console.WriteLine("Commands:");
	Console.WriteLine("  auth-test [url]");
	Console.WriteLine("  order-download [--token <bearer>] [--metadata-uuid <uuid>] [--area-code <code>] [--area-name <name>] [--area-type <type>] [--projection-code <code>] [--projection-name <name>] [--projection-codespace <uri>] [--format-name <name>] [--usage-group <value>] [--usage-purpose <value>] [--software-client <value>] [--software-client-version <value>] [--email <value>] [--output-dir <dir>]");
	Console.WriteLine("  capabilities <metadataUuid>");
	Console.WriteLine("  areas <metadataUuid>");
	Console.WriteLine("  projections <metadataUuid>");
	Console.WriteLine("  formats <metadataUuid>");
	Console.WriteLine("  can-download <metadataUuid> <coordinateSystem> <coordinates>");
	Console.WriteLine("  order-create <orderRequest.json>");
	Console.WriteLine("  order-get <referenceNumber>");
	Console.WriteLine("  download-file <referenceNumber> <fileId> <destinationPath>");
}

static async Task RunOrderDownloadAsync(
	GeoNorgeDownloadClient client,
	OrderDownloadOptions options,
	string? username,
	string? password,
	BearerTokenStore bearerTokenStore)
{
	string token = options.Token;
	if (string.IsNullOrWhiteSpace(token))
	{
		string? envToken = Environment.GetEnvironmentVariable("GEONORGE_BEARER_TOKEN");
		if (!string.IsNullOrWhiteSpace(envToken))
		{
			token = envToken;
		}
	}

	if (string.IsNullOrWhiteSpace(token))
	{
		StoredBearerToken? cachedToken = bearerTokenStore.LoadValid();
		if (cachedToken is not null)
		{
			token = cachedToken.AccessToken;
			Console.WriteLine($"Using cached bearer token (valid until {cachedToken.ExpiresAtUtc:O}).");
		}
	}

	if (string.IsNullOrWhiteSpace(token))
	{
		if (string.IsNullOrWhiteSpace(username) || password is null)
		{
			throw new InvalidOperationException("Missing credentials. Provide --username/--password (or env vars) to acquire token.");
		}

		GeoNorgeBearerTokenAcquirer.TokenAcquisitionResult acquiredToken = await GeoNorgeBearerTokenAcquirer.AcquireBearerTokenAsync(
			options.BaseUrl,
			options.MetadataUuid,
			username,
			password);

		token = acquiredToken.AccessToken;
		bearerTokenStore.Save(new StoredBearerToken
		{
			AccessToken = acquiredToken.AccessToken,
			ExpiresAtUtc = acquiredToken.ExpiresAtUtc
		});

		Console.WriteLine("Acquired bearer token from username/password.");
	}

	var request = new OrderRequest
	{
		Email = options.Email,
		UsageGroup = options.UsageGroup,
		SoftwareClient = options.SoftwareClient,
		SoftwareClientVersion = options.SoftwareClientVersion,
		OrderLines =
		[
			new OrderLineRequest
			{
				MetadataUuid = options.MetadataUuid,
				Areas =
				[
					new AreaSelection
					{
						Code = options.AreaCode,
						Name = options.AreaName,
						Type = options.AreaType
					}
				],
				Projections =
				[
					new ProjectionOption
					{
						Code = options.ProjectionCode,
						Name = options.ProjectionName,
						Codespace = options.ProjectionCodespace
					}
				],
				Formats =
				[
					new FormatOption
					{
						Code = options.FormatCode,
						Name = options.FormatName,
						Type = options.FormatType
					}
				],
				UsagePurpose = [options.UsagePurpose]
			}
		]
	};

	OrderResponse order = await client.CreateOrderV3Async(request, token);
	Console.WriteLine($"Order created: {order.ReferenceNumber}");

	if (!order.Files.Any())
	{
		OrderResponse refreshed = await client.GetOrderV3Async(order.ReferenceNumber, token);
		order = refreshed;
	}

	if (!order.Files.Any())
	{
		Console.WriteLine("Order created but no files were returned.");
		return;
	}

	Directory.CreateDirectory(options.OutputDir);

	foreach (OrderFile file in order.Files)
	{
		if (!string.Equals(file.Status, "ReadyForDownload", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine($"Skipping file with status '{file.Status}': {file.Name}");
			continue;
		}

		if (string.IsNullOrWhiteSpace(file.DownloadUrl))
		{
			Console.WriteLine($"Skipping file without download url: {file.Name}");
			continue;
		}

		string destinationPath = Path.Combine(options.OutputDir, file.Name ?? $"{file.FileId}.zip");
		await client.DownloadFromUrlAsync(file.DownloadUrl, destinationPath, token);
		Console.WriteLine($"Downloaded: {destinationPath}");
	}
}

static OrderDownloadOptions ParseOrderDownloadOptions(string[] args, OrderDownloadOptions defaults)
{
	var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	for (int i = 0; i < args.Length; i++)
	{
		string arg = args[i];
		if (!arg.StartsWith("--", StringComparison.Ordinal))
		{
			throw new ArgumentException($"Unknown argument: {arg}");
		}

		if (i + 1 >= args.Length)
		{
			throw new ArgumentException($"Missing value for {arg}");
		}

		values[arg] = args[i + 1];
		i++;
	}

	return defaults with
	{
		Token = GetOption(values, "--token", defaults.Token),
		MetadataUuid = GetOption(values, "--metadata-uuid", defaults.MetadataUuid),
		AreaCode = GetOption(values, "--area-code", defaults.AreaCode),
		AreaName = GetOption(values, "--area-name", defaults.AreaName),
		AreaType = GetOption(values, "--area-type", defaults.AreaType),
		ProjectionCode = GetOption(values, "--projection-code", defaults.ProjectionCode),
		ProjectionName = GetOption(values, "--projection-name", defaults.ProjectionName),
		ProjectionCodespace = GetOption(values, "--projection-codespace", defaults.ProjectionCodespace),
		FormatName = GetOption(values, "--format-name", defaults.FormatName),
		FormatCode = GetOption(values, "--format-code", defaults.FormatCode),
		FormatType = GetOption(values, "--format-type", defaults.FormatType),
		UsageGroup = GetOption(values, "--usage-group", defaults.UsageGroup),
		UsagePurpose = GetOption(values, "--usage-purpose", defaults.UsagePurpose),
		SoftwareClient = GetOption(values, "--software-client", defaults.SoftwareClient),
		SoftwareClientVersion = GetOption(values, "--software-client-version", defaults.SoftwareClientVersion),
		Email = GetOption(values, "--email", defaults.Email),
		OutputDir = GetOption(values, "--output-dir", defaults.OutputDir)
	};
}

static string GetOption(Dictionary<string, string> values, string key, string fallback)
{
	return values.TryGetValue(key, out string? value) ? value : fallback;
}

static string BuildDefaultAuthTestUrl(string? username, string? password)
{
	if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
	{
		throw new ArgumentException("auth-test requires credentials.");
	}

	string encodedUser = Uri.EscapeDataString(username);
	string encodedPass = Uri.EscapeDataString(password);
	return $"https://httpbin.org/basic-auth/{encodedUser}/{encodedPass}";
}

static async Task RunAuthTestAsync(string url, string? username, string? password)
{
	if (string.IsNullOrWhiteSpace(username) || password is null)
	{
		throw new ArgumentException("auth-test requires username/password.");
	}

	using var client = new HttpClient();
	string raw = $"{username}:{password}";
	string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
	client.DefaultRequestHeaders.Authorization = new("Basic", encoded);

	using var response = await client.GetAsync(url);
	string body = await response.Content.ReadAsStringAsync();

	Console.WriteLine($"Auth test status: {(int)response.StatusCode} {response.ReasonPhrase}");
	if (response.IsSuccessStatusCode)
	{
		Console.WriteLine("Basic auth header accepted.");
	}
	else
	{
		Console.WriteLine("Basic auth test failed.");
		Console.WriteLine(body);
		Environment.ExitCode = 1;
	}
}

static string ReadPassword()
{
	var buffer = new StringBuilder();
	while (true)
	{
		ConsoleKeyInfo key = Console.ReadKey(intercept: true);

		if (key.Key == ConsoleKey.Enter)
		{
			break;
		}

		if (key.Key == ConsoleKey.Backspace)
		{
			if (buffer.Length > 0)
			{
				buffer.Length--;
			}
			continue;
		}

		if (!char.IsControl(key.KeyChar))
		{
			buffer.Append(key.KeyChar);
		}
	}

	return buffer.ToString();
}

static ParsedArgs ParseCliArgs(string[] args)
{
	var commandArgs = new List<string>();
	string? username = Environment.GetEnvironmentVariable("GEONORGE_USERNAME");
	string? password = Environment.GetEnvironmentVariable("GEONORGE_PASSWORD");
	string baseUrl = Environment.GetEnvironmentVariable("GEONORGE_BASE_URL") ?? "https://nedlasting.geonorge.no";

	for (int i = 0; i < args.Length; i++)
	{
		string arg = args[i];

		if (arg == "--username")
		{
			username = ReadOptionValue(args, ref i, "--username");
			continue;
		}

		if (arg == "--password")
		{
			password = ReadOptionValue(args, ref i, "--password");
			continue;
		}

		if (arg == "--base-url")
		{
			baseUrl = ReadOptionValue(args, ref i, "--base-url");
			continue;
		}

		commandArgs.Add(arg);
	}

	if (!string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
	{
		throw new ArgumentException("Username was provided but password is missing. Set --password or GEONORGE_PASSWORD.");
	}

	if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
	{
		throw new ArgumentException("Password was provided but username is missing. Set --username or GEONORGE_USERNAME.");
	}

	return new ParsedArgs(commandArgs.ToArray(), username, password, baseUrl);
}

static string ReadOptionValue(string[] args, ref int index, string optionName)
{
	int valueIndex = index + 1;
	if (valueIndex >= args.Length)
	{
		throw new ArgumentException($"Missing value for {optionName}");
	}

	index = valueIndex;
	return args[valueIndex];
}

internal sealed record ParsedArgs(string[] CommandArgs, string? Username, string? Password, string BaseUrl);

internal sealed record OrderDownloadOptions(
	string BaseUrl,
	string Token,
	string MetadataUuid,
	string AreaCode,
	string AreaName,
	string AreaType,
	string ProjectionCode,
	string ProjectionName,
	string ProjectionCodespace,
	string FormatCode,
	string FormatName,
	string FormatType,
	string UsageGroup,
	string UsagePurpose,
	string SoftwareClient,
	string SoftwareClientVersion,
	string Email,
	string OutputDir)
{
	public static OrderDownloadOptions Default(string baseUrl)
	{
		return new OrderDownloadOptions(
			baseUrl,
			string.Empty,
			"8b4304ea-4fb0-479c-a24d-fa225e2c6e97",
			"3901",
			"Horten",
			"kommune",
			"5972",
			"EUREF89 UTM sone 32, 2d + NN2000",
			"http://www.opengis.net/def/crs/EPSG/0/5972",
			string.Empty,
			"GML",
			string.Empty,
			"næringsliv",
			"tekoginnovasjon",
			"Kartkatalogen",
			"15.7.2821",
			string.Empty,
			Path.Combine(Directory.GetCurrentDirectory(), "downloads"));
	}
}
