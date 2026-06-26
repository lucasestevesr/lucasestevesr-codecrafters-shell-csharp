class Program
{
    static void Main()
    {
        List<string> commands = new List<string>{ "exit", "echo", "type"};
        var path = Environment.GetEnvironmentVariable("PATH");
        string[] folders = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(path))
        { 
            char separator = OperatingSystem.IsWindows() ? ';' : ':';
            folders = path!.Split(separator);
        }
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine();
            
            if (command == commands[0])
            {
                break;
            }

            if (command.StartsWith("echo "))
            {
                Console.WriteLine(command.Substring(5));
            }
            else if (command.StartsWith("type "))
            {
                if (commands.Contains(command.Substring(5)))
                {
                    Console.WriteLine($"{command.Substring(5)} is a shell builtin");
                    continue;
                }
                else
                {
                    foreach (var folder in folders)
                    {
                        var filePath = Path.Combine(folder, command.Substring(5));
                        if (File.Exists(filePath) && File.GetUnixFileMode(filePath).HasFlag(UnixFileMode.UserExecute))
                        {
                            Console.WriteLine($"{command.Substring(5)} is {filePath}");
                            break;
                        }
                    }
                    Console.WriteLine($"{command.Substring(5)}: not found");
                    continue;
                }
            }
            else
                Console.WriteLine($"{command}: command not found");
        }
    }
}

