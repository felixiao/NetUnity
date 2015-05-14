using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Net.NetworkInformation;

namespace NetworkTest
{
    public class TCPServer
    {
        private static TcpListener _listener;
        private static int _port;
        //private static IPAddress _address;
        public static List<ServerClient> clients = new List<ServerClient>();
        public static IPEndPoint serverIEP;
        public TCPServer()
        {
        }

        public void Init()
        {
            _port = 11791;
            foreach (IPAddress IP in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (!IP.IsIPv6LinkLocal)
                {
                    Console.WriteLine(IP.AddressFamily + ", " + IP.IsIPv6LinkLocal + ", " + IP.ToString());
                    _listener = new TcpListener(IP, _port);
                    break;
                }

            }
            
            Console.WriteLine("TCP SERVER: "+_listener.LocalEndpoint.ToString());
            serverIEP = (IPEndPoint)_listener.LocalEndpoint;
            
            _listener.Start();
            _listener.BeginAcceptTcpClient(new AsyncCallback(Accept), null);

        }
        public void Accept(IAsyncResult ar)
        {
            try
            {
                ServerClient sc = new ServerClient(_listener.EndAcceptTcpClient(ar));
                clients.Add(sc);
                
                Console.WriteLine("Acc:" + sc.clientEndPoint.Address.ToString()+":"+sc.clientEndPoint.Port.ToString());
                    //ServerClient client=new ServerClient( _listener.AcceptTcpClient() );
                _listener.BeginAcceptTcpClient(new AsyncCallback(Accept), null);
            }
            catch (Exception e)
            {
                Console.WriteLine("On Listening:" + e.Message);
            }
        }
        public void Recieve()
        {
        }
        public void Send()
        {
        }
        public void SendTo()
        {
        }
        public void Close()
        {
            try
            {
                _listener.Stop();
                //t_listen.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            
            
        }
        public class ServerClient
        {
            public TcpClient client;
            public IPEndPoint clientEndPoint;
            public string name;
            public ServerClient(TcpClient c )
            {
                client = c;
                clientEndPoint = (IPEndPoint)c.Client.RemoteEndPoint;
            }
        }
    }


    public class TCPClient
    {
        TcpClient client;
        IPEndPoint serverIEP;
        public TCPClient()
        {
        }
        public void Init(IPEndPoint server)
        {
            Ping p = new Ping();
            PingReply reply = p.Send(server.Address);
            if (reply.Status != IPStatus.Success)
            {
                throw new PingException(reply.Status.ToString());
            }
            client = new TcpClient(server.Address.ToString(), server.Port);
            Console.WriteLine("TCP Client: "+client.Client.LocalEndPoint.ToString());
            serverIEP = server;
        }
        public void Connect()
        {
            try
            {
                client.Connect(serverIEP);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void Recieve()
        {
        }
        public void Send()
        {
        }
        public void SendTo()
        {
        }
        public void Close()
        {
            client.Close();
        }
    }
}
