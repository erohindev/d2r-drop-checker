﻿using System;
using D2Tools.Helpers;

namespace DropChecker
{
    internal class Program
    {
        public static event Action<ConsoleKeyInfo> OnKeyPressed;

        public static void Main(string[] args)
        {
            Console.WriteLine("D2R DropChecker v0.2");
            
            using var itemChecker = new ItemChecker();
            
            itemChecker.Run();

            //System.Threading.Thread.Sleep(-1);

            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(false);
                
                OnKeyPressed?.Invoke(key);
                
            } while (key.Key != ConsoleKey.Escape);

            GameManager.Run = false;
        }
    }
}