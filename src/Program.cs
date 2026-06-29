using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Linq;

class Program
{
    private static List<string> _builtinCommands = new List<string> { "exit", "echo", "type", "pwd", "cd" };
    private static char[] _specialChars = new char[] { '\\', '"', '$', '`' };
    private static readonly HashSet<string> RedirectionOperators = new() { ">", "1>", "2>", ">>", "1>>", "2>>" };

    static void Main()
    {

        var path = Environment.GetEnvironmentVariable("PATH");
        var home = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : Environment.GetEnvironmentVariable("HOME") ?? "";

        string[] dirs = GetPathDirs(path);
        string filePath;

        while (true)
        {
            Console.Write("$ ");
            string? command = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            string[] args = ParseCommandLine(command);

            (args, var redirection) = ParseRedirection(args);

            string commandName = args[0];
            string[] commandArgs = args[1..];


            switch (commandName)
            {
                case "exit":
                    return;

                case "echo":
                    WriteBuiltinStdout(string.Join(" ", commandArgs), redirection);
                    break;

                case "pwd":
                    WriteBuiltinStdout(Directory.GetCurrentDirectory(), redirection);
                    break;

                case "cd":
                    if (commandArgs[0] == "~")
                        Directory.SetCurrentDirectory(home);
                    else if (Directory.Exists(commandArgs[0]))
                        Directory.SetCurrentDirectory(commandArgs[0]);
                    else
                        Console.WriteLine($"cd: {commandArgs[0]}: No such file or directory");
                    break;

                case "type":

                    var name = commandArgs[0];
                    string output = "";
                    if (_builtinCommands.Contains(name))
                    {
                        output += ($"{name} is a shell builtin");
                    }
                    else if (TryFindExecutablePath(dirs, name, out filePath))
                    {
                        output += ($"{name} is {filePath}");
                    }
                    else
                    {
                        output += ($"{name}: not found");
                    }
                    WriteBuiltinStdout(output, redirection);
                    break;

                default:
                    ExecuteExternalCommand(dirs,commandName, commandArgs, redirection);
                    break;
            }
        }
    }

    private static void ExecuteExternalCommand(string [] dirs, string commandName, string[] commandArgs, Redirection redirection)
    {

        if (!TryFindExecutablePath(dirs, commandName, out _))
        { 
            Console.WriteLine($"{commandName}: command not found");
            return;
        }


        if (redirection.Type == RedirectionType.None || redirection.FilePath is null)
        {
           Process.Start(commandName, commandArgs).WaitForExit();
           return;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = commandName,
            UseShellExecute = false,
            RedirectStandardOutput = redirection.Type == RedirectionType.Stdout,
            RedirectStandardError = redirection.Type == RedirectionType.Stderr
        };
            
        foreach (var arg in commandArgs)
        { 
            processStartInfo.ArgumentList.Add(arg);
        }

        try
        { 
            using (var process = Process.Start(processStartInfo))
            {
                if (process is null)
                {
                    Console.WriteLine($"{commandName}: command not found");
                    return;
                }

                if (redirection.Type == RedirectionType.Stdout)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    WriteRawToFile(output, redirection.FilePath, redirection.WriteMode);
                }
                else if (redirection.Type == RedirectionType.Stderr)
                {
                    string error = process.StandardError.ReadToEnd();
                    WriteRawToFile(error, redirection.FilePath, redirection.WriteMode);
                }
                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{commandName}: {ex.Message}");
        }
    }

    private static bool TryFindExecutablePath(string[] dirs, string commandName, out string path)
    {
        foreach (var dir in dirs)
        {
            var filePath = Path.Combine(dir, commandName);
            if (File.Exists(filePath))
            {
                if (OperatingSystem.IsWindows())
                {
                    path = filePath;
                    return true;
                }

                try
                {
                    if (File.GetUnixFileMode(filePath).HasFlag(UnixFileMode.UserExecute))
                    {
                        path = filePath;
                        return true;
                    }
                }
                catch
                {

                }
            }
        }
        path = string.Empty;
        return false;
    }

    private static string[] GetPathDirs(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<string>();

        char separator = OperatingSystem.IsWindows() ? ';' : ':';
        return path.Split(separator);
    }

    /// <summary>
    /// Parses a command line input into arguments using a simple state machine,Arguments
    /// handling quotes and escape characters.
    /// </summary>
    /// <param name="command">The command line input to parse.</param>
    /// <returns>An array of parsed command arguments.</returns>
    private static string[] ParseCommandLine(string command)
    {
        List<string> args = new List<string>();
        StringBuilder current = new StringBuilder();

        bool insideSingleQuotes = false;
        bool insideDoubleQuotes = false;
        bool escapeNextChar = false;
        bool hasCurrentArg = false;

        for (int i = 0; i < command.Length; i++)
        {
            if (escapeNextChar)
            {
                current.Append(command[i]);
                escapeNextChar = false;
                hasCurrentArg = true;
                continue;
            }
            else if (command[i] == '\\' && !insideDoubleQuotes && !insideSingleQuotes)
            {
                escapeNextChar = true;
                continue;
            }
            else if (command[i] == '\\' && insideDoubleQuotes)
            {
                if (i + 1 < command.Length && _specialChars.Contains(command[i + 1]))
                {
                    current.Append(command[i + 1]);
                    hasCurrentArg = true;
                    i++; // skip the next character since it's already processed.
                }
                else
                {
                    current.Append(command[i]);
                    hasCurrentArg = true;
                }
                continue;
            }
            else if (command[i] == '\'' && !insideDoubleQuotes)
            {
                insideSingleQuotes = !insideSingleQuotes;
                hasCurrentArg = true;
            }
            else if (command[i] == '"' && !insideSingleQuotes)
            {
                insideDoubleQuotes = !insideDoubleQuotes;
                hasCurrentArg = true;
            }
            else if (char.IsWhiteSpace(command[i]) && !insideSingleQuotes && !insideDoubleQuotes)
            {
                if (hasCurrentArg)
                {
                    args.Add(current.ToString());
                    current.Clear();
                    hasCurrentArg = false;
                }
            }
            else
            {
                current.Append(command[i]);
                hasCurrentArg = true;
            }
        }
        if (hasCurrentArg)
        {
            args.Add(current.ToString());
        }
        return args.ToArray();
    }

    /// <summary>
    /// Extracts redirection operators from command arguments.
    /// </summary>
    /// <remarks>
    /// Separates command tokens from redirection information. Supported operators: ">", "1>", "2>".
    /// Returns all arguments without redirection if no operator is found.
    /// </remarks>
    /// <param name="args">Command arguments potentially containing a redirection operator</param>
    /// <returns>
    /// Tuple containing:
    /// - commandTokens: Command arguments before the operator
    /// - redirection: Redirection type and target file path
    /// </returns>
    private static (string[] commandTokens, Redirection redirection) ParseRedirection(string[] args)
    {
        int index = Array.FindIndex(args, arg => RedirectionOperators.Contains(arg));
        if (index == -1)
        {
            return (args, new Redirection(RedirectionType.None, RedirectionWriteMode.None, null));
        }

        string op = args[index];
        RedirectionType type = GetRedirectionType(op);
        RedirectionWriteMode writeMode = GetRedirectionWriteMode(op);

        string? filePath = index + 1 < args.Length
        ? args[index + 1]
        : null;

        string[] commandTokens = args.Take(index).ToArray();

        return (commandTokens, new Redirection(type,writeMode, filePath));
    }

    private static void WriteBuiltinStdout(string output, Redirection redirection)
    {
        try
        {
            if (redirection.Type == RedirectionType.Stdout && redirection.FilePath is not null)
            {
                WriteTextToFile(
                    redirection.FilePath,
                    output + Environment.NewLine,
                    redirection.WriteMode
                );

                return;
            }

            Console.WriteLine(output);

            if (redirection.Type == RedirectionType.Stderr && redirection.FilePath is not null)
            {
                WriteTextToFile(
                    redirection.FilePath,
                    string.Empty,
                    redirection.WriteMode
                );
            }
        }
        catch
        {
            Console.WriteLine($"{redirection.FilePath}: No such file or directory");
        }
    }

    private static void WriteTextToFile(string filePath, string contents, RedirectionWriteMode writeMode)
    {
        if (writeMode == RedirectionWriteMode.Append)
        {
            File.AppendAllText(filePath, contents);
        }
        else
        {
            File.WriteAllText(filePath, contents);
        }
    }

    private static void WriteRawToFile(string output, string filePath, RedirectionWriteMode writeMode)
    {
        if (writeMode == RedirectionWriteMode.Append)
        {
            try
            {
                File.AppendAllText(filePath, output);
            }
            catch
            {
                Console.WriteLine($"{filePath}: No such file or directory");
            }
        }
        else
        {
            try
            {
                File.WriteAllText(filePath, output);
            }
            catch
            {
                Console.WriteLine($"{filePath}: No such file or directory");
            }
        }
    }

    private static RedirectionType GetRedirectionType(string op)
    {
        return op.StartsWith("2")
            ? RedirectionType.Stderr
            : RedirectionType.Stdout;
    }

    private static RedirectionWriteMode GetRedirectionWriteMode(string op)
    {
        return op.Contains(">>")
            ? RedirectionWriteMode.Append
            : RedirectionWriteMode.Overwrite;
    }
}

record Redirection(RedirectionType Type, RedirectionWriteMode WriteMode, string? FilePath);

public enum RedirectionType
{
    None,
    Stdout,
    Stderr
}

public enum RedirectionWriteMode
{
    None,
    Overwrite,
    Append,
}

