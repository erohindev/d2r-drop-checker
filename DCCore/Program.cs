using System;
using D2Tools.Helpers;

namespace DropChecker
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("D2R DropChecker v0.1");
            Console.WriteLine("--------------------");
            
            using var itemChecker = new ItemChecker();
            
            itemChecker.Run();

            //System.Threading.Thread.Sleep(-1);

            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(false);
            } while (key.Key != ConsoleKey.Escape);

            GameManager.Run = false;
        }
    }
}