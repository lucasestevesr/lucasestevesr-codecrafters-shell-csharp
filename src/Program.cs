using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Linq;

class Program
{
    private static List<string> _builtinCommands = new List<string> { "exit", "echo", "type", "pwd", "cd" };
    private static char[] _specialChars = new char[] { '\\', '"', '$', '`' };

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
            string? stdoutFilePath = null;

            bool hasRedirection = args.Any(arg => arg == ">" || arg == "1>");
            if (hasRedirection)
            {
                (args, stdoutFilePath) = HandleRedirection(args);
            }

            string commandName = args[0];
            string[] commandArgs = args[1..];


            switch (commandName)
            {
                case "exit":
                    return;

                case "echo":
                    WriteStdout(string.Join(" ", commandArgs), stdoutFilePath);
                    break;

                case "pwd":
                    WriteStdout(Directory.GetCurrentDirectory(), stdoutFilePath);
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
                    WriteStdout(output, stdoutFilePath);
                    break;

                default:
                    ExecuteExternalCommand(dirs,commandName, commandArgs, stdoutFilePath);
                    break;
            }
        }
    }

    private static void ExecuteExternalCommand(string [] dirs, string commandName, string[] commandArgs, string? stdoutFilePath)
    {
        string executablePath;
       
        if (!TryFindExecutablePath(dirs, commandName, out executablePath))
        { 
            Console.WriteLine($"{commandName}: command not found");
            return;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = commandName,
            ArgumentList = { string.Join(" ", commandArgs) }, 
        };

        if (stdoutFilePath is null)
        {
           Process.Start(commandName, commandArgs).WaitForExit();
        }
        else
        {
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            try
            { 
                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine($"{commandName}: command not found");
                        return;
                    }
                    if (stdoutFilePath != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        WriteStdout(output, stdoutFilePath);
                    }
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{commandName}: {ex.Message}");
            }
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

    private static (string[] commandTokens, string? stdoutFile) HandleRedirection(string[] args)
    {
        int index = Array.FindIndex(args, arg => arg == ">" || arg == "1>");
        var commandTokens = args.Take(index).ToArray();

        if (index + 1 >= args.Length)
            return (commandTokens, null);

        var stdoutFile = args[index + 1];

        return (commandTokens, stdoutFile);
    }

    private static void WriteStdout(string output, string? stdoutFilePath)
    {
        if (stdoutFilePath == null)
        {
            Console.WriteLine(output);
            return;
        }
        try
        {
            File.WriteAllText(stdoutFilePath, output + Environment.NewLine);
        }
        catch
        {
            Console.WriteLine($"{stdoutFilePath}: No such file or directory");
        }
    }
}

