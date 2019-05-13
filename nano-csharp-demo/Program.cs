using starx_client_dotnet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// SimpleJSON 参考: http://wiki.unity3d.com/index.php/SimpleJSON
namespace nano_csharp_demo
{
    class Program
    {
        private static DateTime JanFirst1970 = new DateTime(1970, 1, 1);
        private static long getTime()
        {
            return (long)((DateTime.Now.ToUniversalTime() - JanFirst1970).TotalMilliseconds + 0.5);
        }

        private static string getNickName()
        {
            // Console.WriteLine(DateTime.Now.ToFileTime());
            // string guid = Guid.NewGuid().ToString("N");
            // Console.WriteLine(guid);
            // Console.WriteLine(getTime());
            return string.Format("client-{0}", getTime());
        }

        private static StarXClient client;
        private static ManualResetEvent TerminationRequestedEvent;
        private static bool connected = false;
        private static bool completed = false;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);

            TerminationRequestedEvent = new ManualResetEvent(false);

            client = new StarXClient();
            client.NetWorkStateChangedEvent += (NetWorkState state) => {
                if (completed)
                {
                    Console.WriteLine();
                }
                if(state == NetWorkState.CONNECTED)
                {
                    connected = true;
                }
                Console.Write("status: ");
                Console.WriteLine(state);
            };
            client.Init("127.0.0.1", 3250, () =>
            {
                // Console.WriteLine("init client callback");
                client.Connect((data) =>
                {
                    Console.Write("Connect-response: ");
                    Console.WriteLine(Encoding.UTF8.GetString(data));
                    // Console.WriteLine("connect client callback");

                    // 服务器主动推送消息-新用户加入
                    client.On("onNewUser", (m) =>
                    {
                        Console.WriteLine("onNewUser-message: " + Encoding.UTF8.GetString(m));
                    });

                    // 服务器主动推送消息-用户离开
                    client.On("onUserLeft", (m) =>
                    {
                        Console.WriteLine("onUserLeft-message: " + Encoding.UTF8.GetString(m));
                    });
                    // 服务器主动推送消息-成员数更新
                    client.On("onMembers", (m) =>
                    {
                        Console.WriteLine("onMembers-message: " + Encoding.UTF8.GetString(m));
                    });

                    // 请求服务器，room.join 信息
                    client.Request("room.join", Encoding.UTF8.GetBytes("{}"), (resp) =>
                    {
                        Console.WriteLine("room.join-response: " + Encoding.UTF8.GetString(resp));
                    });
                });
            });


            string nickname = getNickName();
            while (!TerminationRequestedEvent.WaitOne(0))
            {
                Thread.Sleep(50);
                if(connected)
                {
                    Console.Write(string.Format("{0}> ", nickname));
                    string message = Console.ReadLine();
                    Dictionary<string, string> room_message = new Dictionary<string, string>();
                    room_message.Add("name", nickname);
                    room_message.Add("content", message);
                    // 通知服务器，room.message 信息
                    client.Notify("room.message", Encoding.UTF8.GetBytes(SimpleJson.SerializeObject(room_message)));
                }
                Thread.Sleep(50);
            }
            Thread.Sleep(50);
            Console.Write("Press any key to exit...");
            Console.ReadKey(true);
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            if (!completed) {
                TerminationRequestedEvent.Set();
                args.Cancel = true;
                completed = true;
                Thread.Sleep(50);
                client.Disconnect();
            }
        }
    }
}
