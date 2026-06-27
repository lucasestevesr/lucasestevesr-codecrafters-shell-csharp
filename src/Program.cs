using System.Diagnostics;
using System.Text;

class Program
{
    private static List<string> _builtinCommands = new List<string> { "exit", "echo", "type", "pwd", "cd"};
    private static char[] _specialChars = new char[] { '\\', '"', '$', '`'};

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
            string commandName = args[0];
            string[] commandArgs = args[1..];

            switch (commandName)
            { 
                case "exit":
                    return;

                case "echo":
                    Console.WriteLine(string.Join(" ", commandArgs));
                    break;

                case "pwd":
                    Console.WriteLine(Directory.GetCurrentDirectory());
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

                    if (_builtinCommands.Contains(name))
                    {
                        Console.WriteLine($"{name} is a shell builtin");
                    }
                    else if (TryFindExecutablePath(dirs, name, out filePath))
                    {
                        Console.WriteLine($"{name} is {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"{name}: not found");
                    }
                    break;
               
                default:
                    if (TryFindExecutablePath(dirs, commandName, out filePath))
                    {
                        Process.Start(commandName, commandArgs).WaitForExit();
                    }
                    else
                    {
                        Console.WriteLine($"{command}: command not found");
                    }
                    break;
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
    /// Parses a command line input into arguments using a simple state machine,
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

        for (int i=0; i < command.Length; i++)
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
}

