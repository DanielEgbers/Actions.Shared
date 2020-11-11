#r "nuget: SimpleExec, 6.2.0"

#nullable enable

using SimpleExec;
using System.Text.RegularExpressions;

public class GitChange
{
    public string File { get; set; } = string.Empty;
}

public static class Git
{
    public static async Task ConfigUserAsync(string name, string email, string? workingDirectory = null)
    {
        await Command.RunAsync("git", $"config user.name \"{name}\"", workingDirectory: workingDirectory);
        await Command.RunAsync("git", $"config user.email \"{email}\"", workingDirectory: workingDirectory);
    }

    public static async Task<IEnumerable<GitChange>> GetChangesAsync(string? workingDirectory = null)
    {
        var gitStatus = Regex.Replace(await Command.ReadAsync("git", $"status --short --untracked-files", workingDirectory: workingDirectory), @"\r\n?|\n", Environment.NewLine).Trim().Split(Environment.NewLine);
        return gitStatus.Select(l => Regex.Match(l, "^([^ ]+)[ ]+(.*)$")).Select(m => new GitChange() { File = m.Groups[2].Value });
    }

    public static async Task StageAllAsync(string? workingDirectory = null)
    {
        await Command.RunAsync("git", $"add --all", workingDirectory: workingDirectory);
    }

    public static async Task<bool> TryStageAsync(string pathspec, string? workingDirectory = null)
    {
        try
        {
            await Command.RunAsync("git", $"add {pathspec}", workingDirectory: workingDirectory);
            return true;
        }
        catch (NonZeroExitCodeException)
        {
            return false;
        }
    }

    public static async Task CommitAsync(string message, string? workingDirectory = null, DateTimeOffset? date = null)
    {
        await Command.RunAsync("git", $"commit --message=\"{message}\" {(date != null ? $"--date={date.Value.ToUnixTimeSeconds()}" : string.Empty)}", workingDirectory: workingDirectory);
    }

    public static async Task PushAsync(string? workingDirectory = null)
    {
        await Command.RunAsync("git", $"push --quiet --progress", workingDirectory: workingDirectory);
    }
}