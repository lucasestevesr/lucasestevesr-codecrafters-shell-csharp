class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            var command = Console.ReadLine();
            if (command == "exit")
            {
                break;
            }
            Console.Write($"{command}: command not found\n");
            //Console.Clear();
        }
    }
}
