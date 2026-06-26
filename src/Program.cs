class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            var command = Console.ReadLine();
            Console.Write($"{command}: command not found\n");
            //Console.Clear();
        }
    }
}
