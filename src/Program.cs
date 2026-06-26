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
            if (command.StartsWith("echo "))
            {
                Console.WriteLine(command.Substring(5));
            }
            Console.WriteLine($"{command}: command not found");
            //Console.Clear();
        }
    }
}
