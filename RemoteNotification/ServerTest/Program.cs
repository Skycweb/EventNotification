using System;
using System.Threading;
using RemoteNotificationCommon;

namespace ServerTest
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Console.WriteLine("开始运行了");
            Service service = new Service();
            var i = Console.Read();
            Console.WriteLine("结束了");
            Thread.Sleep(3);
        }
    }
}
