using System;
using System.Net;
using System.Text;
using System.Threading;
using RemoteNotificationCommon;

namespace TestClient
{
    internal class Program
    {
        static int iii = 0;
        static int k = 0;
        public static void Main(string[] args)
        {
            Console.WriteLine("程序启动");
            Thread.Sleep(1000*5);
            Console.WriteLine("开始运行了");
            Client client = new Client(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10008), "我是测试程序");
            client.ConnectHeadl = () => {
                client.AddEvent("测试", () =>
                {
                    Console.WriteLine("我被通知了"+(iii++));
                });
            };
            Console.WriteLine($"输入c就是关闭程序,p表示发送通知");
            int i = Console.Read();
            char[] inputCmd = new char[] { 'c', 'p' };
            while(i != inputCmd[0])
            {
                Console.WriteLine($"输入c就是关闭程序,p表示发送通知");
                i = Console.Read();
                if (i == inputCmd[1]) {
                    for (int b = 0; b < 100; b++)
                    {
                        client.PostEvent("测试");
                        Console.WriteLine("已经发送通知"+(k++));
                    }
                    
                }
            }
            Console.WriteLine("结束了");
            Thread.Sleep(3);
        }
    }
}
