using System.Diagnostics;
using System.Text;

class Program
{
    private static List<string> _builtinCommands = new List<string> { "exit", "echo", "type", "pwd", "cd"};
    
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

    private static string[] ParseCommandLine(string command)
    {
        List<string> args = new List<string>();
        StringBuilder current = new StringBuilder();

        bool insideSingleQuotes = false;
        bool insideDoubleQuotes = false;
        bool hasCurrentArg = false;

        foreach (char c in command)
        {
            if (c == '\'' && !insideDoubleQuotes)
            {
                insideSingleQuotes = !insideSingleQuotes;
                hasCurrentArg = true;
            }
            else if (c == '"' && !insideSingleQuotes)
            {
                insideDoubleQuotes = !insideDoubleQuotes;
                hasCurrentArg = true;
            }
            else if (char.IsWhiteSpace(c) && !insideSingleQuotes && !insideDoubleQuotes)
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
                current.Append(c);
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

