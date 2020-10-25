#r "nuget: SimpleExec, 6.2.0"

#nullable enable

using SimpleExec;

public static class Git
{
    public static async Task ConfigUserAsync(string? workingDirectory, string name, string email)
    {
        await Command.RunAsync("git", $"config user.name \"{name}\"", workingDirectory: workingDirectory);
        await Command.RunAsync("git", $"config user.email \"{email}\"", workingDirectory: workingDirectory);
    }

    public static async Task<bool> CommitAsync(string? workingDirectory, string message, params string[] files)
    {
        var gitStatus = await Command.ReadAsync("git", $"status --short --untracked-files", workingDirectory: workingDirectory);

        var changedFiles = files.Where(f => gitStatus.Contains(f)).ToArray();

        if (changedFiles.Length <= 0)
            return false;

        var changedFilesJoin = $"\"{string.Join("\" \"", changedFiles)}\"";

        await Command.RunAsync("git", $"add {changedFilesJoin}", workingDirectory: workingDirectory);

        var gitCommitMessage = message.Replace($"{{{nameof(files)}}}", changedFilesJoin.Replace("\"", "'"));
        await Command.RunAsync("git", $"commit -m \"{gitCommitMessage}\"", workingDirectory: workingDirectory);

        return true;
    }

    public static async Task PushAsync(string? workingDirectory)
    {
        await Command.RunAsync("git", "push --quiet --progress", workingDirectory: workingDirectory);
    }
}