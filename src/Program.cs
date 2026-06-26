class Program
{
    static void Main()
    {
        List<string> commands = new List<string>{ "exit", "echo", "type"};
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine();
            if (command == commands[0])
            {
                break;
            }
            else if (command.StartsWith("echo "))
            {
                Console.WriteLine(command.Substring(5));
                continue;
            }
            else if (command.StartsWith("type ") && commands.Contains(command.Substring(5)))
            {
                Console.WriteLine($"{command.Substring(5)} is a shell builtin");
                continue;
            }
            Console.WriteLine($"{command}: command not found");
            //Console.Clear();
        }
    }
}
