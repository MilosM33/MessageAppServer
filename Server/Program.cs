using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        public static int listenCount = 10;
        public static string ip = "192.168.0.101";
        public static int port = 5050;
        public static Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static List<Socket> clients = new List<Socket>();
        public static List<List<string>> messageList = new List<List<string>>();
        public static int maxMessageCount = 50;
        public static byte[] buffer = new byte[2048];
        
        static void Main(string[] args)
        {
            Console.WriteLine("[SERVER] Starting...");
            Setup();

            while (Console.ReadLine() != "stop")
            {
                Thread.Sleep(1000);
            }
        }
        
        public static void Setup()
        {
            Console.WriteLine("[Server] Binding socket");
            IPAddress ipaddress;
            if(IPAddress.TryParse(ip,out ipaddress))
            {
                server.Bind(new IPEndPoint(ipaddress,port));
                Console.WriteLine(string.Format("[SERVER] Listening on {0}:{1}", ipaddress,port));
                server.Listen(listenCount);
                server.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            }
            else
            {
                Console.WriteLine("Invalid ip");
            }
            
        }

        public static void AcceptCallBack(IAsyncResult result)
        {
            Console.WriteLine("New user connected");
            Socket s = server.EndAccept(result);
            clients.Add(s);
            s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), s);
            server.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }
        public static void ReceiveCallBack(IAsyncResult result)
        {
            Socket s = (Socket)result.AsyncState;
            int dataLength = 0;
            try
            {
                 dataLength = s.EndReceive(result);
            }
            catch (Exception)
            {
                Console.WriteLine("User diconnected");
                clients.Remove(s);

                SendTextToAll("!users " + clients.Count);
                return;
                
            }
            byte[] temp = new byte[dataLength];
            Array.Copy(buffer, temp, dataLength);
            string text = Encoding.ASCII.GetString(temp);
            if (messageList.Count > maxMessageCount)
            {
                messageList.RemoveAt(0);
            }
            if (text.Length == 0)
            {
                s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), s);
                return;
            }
                
            if (text[0] != '!') {
                string[] message = text.Split('&');
                if (message.Length == 2)
                {
                    string currentTime = DateTime.Now.ToLongTimeString();
                    messageList.Add(new List<string>() { message[0], currentTime, message[1] });
                    Console.WriteLine(string.Format("{0} [{1}]: {2} ", currentTime, message[0], message[1]));
                    SendTextToAll(text);
                }
            }
            else 
            {
                if(text == "!ready")
                {
                    SendTextToAll("!users " + clients.Count);

                    foreach (var item in messageList)
                    {
                        
                        SendText(string.Format("{0}&{1} ", item[0], item[2]), s);
                    }
             
                }
                    
            }
            s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), s);
           
        }
        
        public static void SendCallBack(IAsyncResult result)
        {   
            Socket s = (Socket)result.AsyncState;
            s.EndSend(result);
               
        }
        public static void SendText(string text,Socket s)
        {
            Thread.Sleep(20);
            byte[] data = Encoding.ASCII.GetBytes(text);
            s.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), s);

        }
        
        public static void SendTextToAll(string text,Socket ignore = null)
        {
            foreach (var item in clients)
            {
                if (item != ignore)
                {
                    SendText(text, item);
                }

            }
        }
    }
}
