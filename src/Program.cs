using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Program
{
    private static List<string> _builtinCommands = new List<string> { "exit", "echo", "type", "pwd", "cd" };
    
    static void Main()
    {
        
        var path = Environment.GetEnvironmentVariable("PATH");
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

            string[] args = command!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandName = args[0];
            string[] commandArgs = args[1..];

            switch (commandName)
            { 
                case "exit":
                    return;

                case "echo":
                    Console.WriteLine(command.Substring(5));
                    break;

                case "pwd":
                    Console.WriteLine(Directory.GetCurrentDirectory());
                    break;

                case "cd":
                    if (Directory.Exists(commandArgs[0]))
                        Directory.SetCurrentDirectory(commandArgs[0]);
                    else
                        Console.WriteLine($"cd: <{commandArgs[0]}>: No such file or directory");
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
}

