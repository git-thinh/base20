using System;
using System.Collections.Generic;
using System.Text;

namespace F88.Mobility.Server.Notify
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "TCP:12345";
            new ServerTcp("127.0.0.1", 12345);
            Console.ReadLine();
        }
    }
}
