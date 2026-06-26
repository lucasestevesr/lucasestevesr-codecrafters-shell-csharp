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
            Console.WriteLine($"{command}: command not found");
            //Console.Clear();
        }
    }
}
