/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GitHub.Copilot.SDK.Test.Harness;

public class E2ETestContext : IAsyncDisposable
{
    public string CliPath { get; }
    public string HomeDir { get; }
    public string WorkDir { get; }
    public string ProxyUrl { get; }

    private readonly CapiProxy _proxy;
    private readonly string _repoRoot;

    private E2ETestContext(string cliPath, string homeDir, string workDir, string proxyUrl, CapiProxy proxy, string repoRoot)
    {
        CliPath = cliPath;
        HomeDir = homeDir;
        WorkDir = workDir;
        ProxyUrl = proxyUrl;
        _proxy = proxy;
        _repoRoot = repoRoot;
    }

    public static async Task<E2ETestContext> CreateAsync()
    {
        var repoRoot = FindRepoRoot();
        var cliPath = GetCliPath(repoRoot);

        var homeDir = Path.Combine(Path.GetTempPath(), $"copilot-test-config-{Guid.NewGuid()}");
        var workDir = Path.Combine(Path.GetTempPath(), $"copilot-test-work-{Guid.NewGuid()}");

        Directory.CreateDirectory(homeDir);
        Directory.CreateDirectory(workDir);

        var proxy = new CapiProxy();
        var proxyUrl = await proxy.StartAsync();

        return new E2ETestContext(cliPath, homeDir, workDir, proxyUrl, proxy, repoRoot);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "nodejs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find repository root");
    }

    private static string GetCliPath(string repoRoot)
    {
        var envPath = Environment.GetEnvironmentVariable("COPILOT_CLI_PATH");
        if (!string.IsNullOrEmpty(envPath)) return envPath;

        var path = Path.Combine(repoRoot, "nodejs/node_modules/@github/copilot/index.js");
        if (!File.Exists(path))
            throw new InvalidOperationException($"CLI not found at {path}. Run 'npm install' in the nodejs directory first.");

        return path;
    }

    public async Task ConfigureForTestAsync(string testFile, [CallerMemberName] string? testName = null)
    {
        // Convert PascalCase method names to snake_case matching snapshot filenames
        // e.g., Should_Create_A_Session_With_AvailableTools -> should_create_a_session_with_availableTools
        var sanitizedName = Regex.Replace(testName!, @"_([A-Z])([A-Z]+)(_|$)", m =>
            "_" + char.ToLowerInvariant(m.Groups[1].Value[0]) + m.Groups[2].Value.ToLowerInvariant() + m.Groups[3].Value);
        sanitizedName = Regex.Replace(sanitizedName, @"(^|_)([A-Z])(?=[a-z]|_|$)", m =>
            m.Groups[1].Value + char.ToLowerInvariant(m.Groups[2].Value[0]));
        var snapshotPath = Path.Combine(_repoRoot, "test", "snapshots", testFile, $"{sanitizedName}.yaml");
        await _proxy.ConfigureAsync(snapshotPath, WorkDir);
    }

    public Task<List<ParsedHttpExchange>> GetExchangesAsync() => _proxy.GetExchangesAsync();

    public IReadOnlyDictionary<string, string> GetEnvironment()
    {
        var env = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => e.Value?.ToString());

        env["COPILOT_API_URL"] = ProxyUrl;
        env["XDG_CONFIG_HOME"] = HomeDir;
        env["XDG_STATE_HOME"] = HomeDir;

        return env!;
    }

    public CopilotClient CreateClient() => new(new CopilotClientOptions
    {
        CliPath = CliPath,
        Cwd = WorkDir,
        Environment = GetEnvironment()
    });

    public async ValueTask DisposeAsync()
    {
        await _proxy.DisposeAsync();

        try { if (Directory.Exists(HomeDir)) Directory.Delete(HomeDir, true); } catch { }
        try { if (Directory.Exists(WorkDir)) Directory.Delete(WorkDir, true); } catch { }
    }
}
