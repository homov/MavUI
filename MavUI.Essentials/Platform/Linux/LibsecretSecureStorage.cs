using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Mav.Essentials.Linux;

/// <summary>
/// SecureStorage on Linux through libsecret's <c>secret-tool</c>. Each secret is an item with
/// attributes <c>service=MavUI.Essentials</c> and <c>account=&lt;key&gt;</c>, stored in the user's keyring.
/// </summary>
/// <remarks>
/// Secret values travel through stdin, never through the command line. Values are base64-encoded so that
/// a trailing newline added by a tool cannot change the data. Without <c>secret-tool</c> or a running
/// Secret Service provider, every call throws <see cref="PlatformNotSupportedException"/> or
/// <see cref="InvalidOperationException"/>; nothing is written to disk.
/// </remarks>
public sealed class LibsecretSecureStorage : IMavSecureStorage
{
    private const string Tool = "secret-tool";
    private const string Service = "MavUI.Essentials";
    private const string Label = "MavUI.Essentials secret";

    public async Task<string?> GetAsync(string key)
    {
        ValidateKey(key);

        var result = await RunAsync(["lookup", "service", Service, "account", key], stdin: null);
        if (result.ExitCode == 1 && result.Stderr.Length == 0 && result.Stdout.Length == 0)
        {
            return null; // secret-tool exits 1 without diagnostics when no item matches
        }

        ThrowIfFailed(result, "lookup");
        return Encoding.UTF8.GetString(Convert.FromBase64String(result.Stdout.Trim()));
    }

    public async Task SetAsync(string key, string value)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        var result = await RunAsync(
            ["store", "--label", Label, "service", Service, "account", key],
            stdin: encoded);
        ThrowIfFailed(result, "store");
    }

    public bool Remove(string key)
    {
        ValidateKey(key);

        var existed = RunAsync(["lookup", "service", Service, "account", key], stdin: null).GetAwaiter().GetResult().ExitCode == 0;
        var result = RunAsync(["clear", "service", Service, "account", key], stdin: null).GetAwaiter().GetResult();
        ThrowIfFailed(result, "clear");
        return existed;
    }

    public void RemoveAll()
    {
        var search = RunAsync(["search", "--all", "service", Service], stdin: null).GetAwaiter().GetResult();
        if (search.ExitCode != 0 && search.Stdout.Length == 0)
        {
            ThrowIfFailed(search, "search");
        }

        foreach (var account in LibsecretOutput.ParseAccounts(search.Stdout))
        {
            var result = RunAsync(["clear", "service", Service, "account", account], stdin: null).GetAwaiter().GetResult();
            ThrowIfFailed(result, "clear");
        }
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (key.Any(char.IsControl))
        {
            throw new ArgumentException("Secure storage keys cannot contain control characters.", nameof(key));
        }
    }

    private static void ThrowIfFailed((int ExitCode, string Stdout, string Stderr) result, string operation)
    {
        if (result.ExitCode != 0)
        {
            // stderr from secret-tool describes the failure and never contains the secret value.
            throw new InvalidOperationException(
                $"secret-tool {operation} failed (exit {result.ExitCode}): {result.Stderr.Trim()}");
        }
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(string[] arguments, string? stdin)
    {
        var startInfo = new ProcessStartInfo(Tool)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("secret-tool could not be started.");
        }
        catch (Win32Exception)
        {
            throw new PlatformNotSupportedException(
                "secret-tool was not found. Install libsecret-tools (for example: sudo apt install libsecret-tools) " +
                "and a Secret Service provider such as GNOME Keyring or KeePassXC.");
        }

        using (process)
        {
            if (stdin is not null)
            {
                await process.StandardInput.WriteAsync(stdin);
            }

            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await stdoutTask, await stderrTask);
        }
    }
}

/// <summary>Parsing helpers for <c>secret-tool</c> output, kept separate so they can be unit-tested.</summary>
public static class LibsecretOutput
{
    private const string AccountPrefix = "attribute.account = ";

    /// <summary>Extracts account names from <c>secret-tool search</c> output.</summary>
    public static IReadOnlyList<string> ParseAccounts(string output)
    {
        var accounts = new List<string>();
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith(AccountPrefix, StringComparison.Ordinal))
            {
                var account = trimmed[AccountPrefix.Length..];
                if (account.Length > 0 && !accounts.Contains(account))
                {
                    accounts.Add(account);
                }
            }
        }

        return accounts;
    }
}
