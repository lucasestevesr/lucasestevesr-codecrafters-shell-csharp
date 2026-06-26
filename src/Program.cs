using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

class Program
{
    private static List<string> _builtinCommands = new List<string> { "exit", "echo", "type" };
    
    static void Main()
    {
        
        var path = Environment.GetEnvironmentVariable("PATH");
        string[] dirs = GetPathDirs(path);
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine();
            string[] args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandName = args[0];
            string[] commandArgs = args[1..];

            if (commandName == "exit")
            {
                break;
            }

            if (commandName == "echo")
            {
                Console.WriteLine(command.Substring(5));
                continue;
            }
            if (commandName == "type")
            {
                var name = command.Substring(5);
                if (_builtinCommands.Contains(name))
                {
                    Console.WriteLine($"{name} is a shell builtin");
                    continue;
                }
                else
                {
                    if (TryFindExecutablePath(dirs, name, out var filePath))
                    {
                        Console.WriteLine($"{name} is {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"{name}: not found");
                    }
                    continue;
                }
            }
            else {
                TryFindExecutablePath(dirs, command, out var filePath);
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    Process.Start(commandName, commandArgs).WaitForExit();
                }
            }

            Console.WriteLine($"{command}: command not found");
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
}

