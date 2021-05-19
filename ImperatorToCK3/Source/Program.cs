using System;

namespace ImperatorToCK3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            if(args.Length > 0)
            {
                Console.WriteLine("ImperatorToCK3 takes no parameters.");
                Console.WriteLine("It uses configuration.txt, configured manually or by the frontend.");
            }
        }
    }
}
