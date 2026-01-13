using System;
using System.Diagnostics;
using System.Reflection;
using Genius2;
using Microsoft.Win32;

namespace RemoteInvoker
{
    public class Program
    {
        private static bool IsRunning = true;

        private static void WaitSomeTime()
        {
            int seconds = 1 * 60 * 60;
            for (int i = 0; i < seconds; i++)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                    {
                        IsRunning = false;
                    }
                    return;
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
        static void Main(string[] args)
        {
            WebService.StartWebService();
            Console.Write("Press any key to exit application");
            while (IsRunning)
            {
                WaitSomeTime();
            }
        }
    }
}
