class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            string command = Console.ReadLine();
            if (command == "exit")
            {
                break;
            }
            else if (command.StartsWith("echo "))
            {
                Console.WriteLine(command.Substring(5));
                continue;
            }
            else if (command.StartsWith("type "))
            {
                Console.WriteLine($"{command.Substring(5)} is a shell builtin");
                continue;
            }
            Console.WriteLine($"{command}: command not found");
            //Console.Clear();
        }
    }
}
