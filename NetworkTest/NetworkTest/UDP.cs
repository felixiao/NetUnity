using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkTest
{
    public class UDP
    {
        public delegate void eventHandler(eventArgs e);

        public static event eventHandler handler;
        public class eventArgs
        {
            public Client client;
            public string token;
            public string msg;
            public eventArgs(Client c, string t, string s)
            {
                client = c;
                token = t;
                msg = s;
            }
        }

        public List<Client> clients = new List<Client>();
        IPEndPoint IepRecv;
        UdpClient receiver, sender;
        public IPEndPoint tcpServer;

        int listenPort = 11791;
        int defaultPort = 11791;
        int sendPortMax = 11791;

        int maxSearchTime = 5;
        int searchTime = 0;
        public bool findServer = false;
        IPEndPoint localSendIEP;
        string name;
        public void Init() 
        {
            try
            {
                bool estServer = true;
                IepRecv = new IPEndPoint(IPAddress.Any, 0);

                while (estServer)
                {
                    try
                    {
                        receiver = new UdpClient(listenPort);
                        estServer = false;
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        Console.WriteLine(e.ToString());
                        //handler(new eventArgs(null, "err", "59" + e.ToString()));
                        listenPort++;
                        sendPortMax++;
                    }
                }

                sender = new UdpClient();
                localSendIEP = new IPEndPoint(IPAddress.None, 0);
                foreach (IPAddress IP in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    
                    if (!IP.IsIPv6LinkLocal)
                    {
                        Console.WriteLine(IP.AddressFamily + ", " + IP.IsIPv6LinkLocal + ", " + IP.ToString());
                        handler(new eventArgs(null, "init", IP.ToString() + ":" + sendPortMax.ToString()));
                        break;
                    }
                        
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                handler(new eventArgs(null, "err", "@84 " + ex.ToString()));
            }
        }
        public void Close()
        {
            receiver.Close();
            sender.Close();
        }
        public void Receive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint e = (IPEndPoint)ar.AsyncState;

                Byte[] receiveBytes = receiver.EndReceive(ar, ref e);
                string receiveString = Encoding.ASCII.GetString(receiveBytes);

                if (receiveString.Length > 0&&e.Address!=localSendIEP.Address&&e.Port!=localSendIEP.Port)
                {
                    
                    string token = receiveString.Split('|')[0];
                    string msg = receiveString.Split('|')[1];
                    if (token == "getport")
                    {
                        Console.WriteLine("Sender IEP is " + e.Address.ToString()+":"+e.Port.ToString());
                        localSendIEP.Address = e.Address;
                        localSendIEP.Port = e.Port;
                        handler(new eventArgs(null, "err", "@111 Local Sender IP " + e.Address.ToString()));
                    }
                    if (token == "conn")
                    {
                        int port = int.Parse(msg.Split(',')[0]);
                        string cname = msg.Split(',')[1];
                        if (sendPortMax < port)
                            sendPortMax = port;
                        clients.Add(new Client(e, cname));
                        SendTo("connacc|" + tcpServer.Address.ToString() + ":" + tcpServer.Port+ "," + name + "," + sendPortMax, new IPEndPoint(e.Address, port));
                    }
                    if (token == "connacc")
                    {
                        findServer = true;
                        string serverIep=msg.Split(',')[0];
                        string cname=msg.Split(',')[1];
                        int portMax=int.Parse(msg.Split(',')[2]);
                        if (sendPortMax < portMax)
                            sendPortMax = portMax;
                        Console.WriteLine("Connection acc: " + cname + " , Server IEP: " + serverIep + ", portMax: " + portMax);
                        tcpServer=new IPEndPoint(IPAddress.Parse(serverIep.Split(':')[0]),int.Parse(serverIep.Split(':')[1]));
                        clients.Add(new Client(e, cname));
                    }

                    Console.WriteLine("Msg: '" + msg + "' From: " + e.Address.ToString() + ":" + e.Port.ToString());
                    foreach (Client c in clients)
                    {
                        Console.WriteLine(c.iep.Address.Equals(e.Address) +" "+ c.iep.Port.Equals(e.Port));
                        if (c.iep.Address.Equals(e.Address) && c.iep.Port.Equals(e.Port))
                        {
                            Console.WriteLine("Msg: '" + msg + "' From: " + c.name);
                            handler(new eventArgs(c, token, msg));
                        }
                    }
                }

                receiver.BeginReceive(new AsyncCallback(Receive), IepRecv);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "@151 " + e.ToString()));
            }
        }
        public void Receive()
        {
            try
            {
                receiver.BeginReceive(new AsyncCallback(Receive), IepRecv);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "@163 " + e.ToString()));
            }
        }
        public void GetSendPort()
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes("getport|");
                foreach (IPAddress IP in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (!IP.IsIPv6LinkLocal)
                    {
                        Console.WriteLine(IP.AddressFamily + ", " + IP.IsIPv6LinkLocal + ", " + IP.ToString());
                        //handler(new eventArgs(null, "err",IP.AddressFamily + ", " + IP.IsIPv6LinkLocal + ", " + IP.ToString()));
                        sender.Send(buffer, buffer.Length, new IPEndPoint(IP, sendPortMax));
                        break;
                    }

                }
                
                //Thread.Sleep(100);//wait for 100ms
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "@188 "+e.ToString()));
            }
        }
        public void SearchServer(string s)
        {
            name = s;
            try
            {
                while (!findServer)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes("conn|" + sendPortMax + "," + name);
                    for (int i = defaultPort; i <= sendPortMax; i++)
                        sender.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Broadcast, i));
                    if (searchTime++ >= maxSearchTime)
                        break;
                    
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "@210 " + e.ToString()));
            }
        }
        public void SendTo(String s, IPEndPoint iep)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(s);
                sender.Send(buffer, buffer.Length, iep);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "@223 " + e.ToString()));
            }
        }
        public void P2PSearch(string s)
        {
            name = s;
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes("conn|" + sendPortMax + "," + name);
                string ip0 = localSendIEP.Address.ToString().Split('.')[0];
                string ip1 = localSendIEP.Address.ToString().Split('.')[1];
                string ip2 = localSendIEP.Address.ToString().Split('.')[2];
                //int ip3 = int.Parse(localSendIEP.Address.ToString().Split('.')[3]);
                string ip = ip0 + "." + ip1 + "." + ip2+".";

                for (int i = defaultPort; i <= sendPortMax; i++)
                {
                    for (int j = 1; j < 256; j++)
                    {
                        IPEndPoint iep = new IPEndPoint(IPAddress.Parse(ip + j.ToString()), i);
                        //Console.WriteLine(iep.ToString());
                        sender.Send(buffer, buffer.Length, iep);
                    }
                        
                }
                Thread.Sleep(100);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "@236 " + e.ToString()));
            }
        }
        public void SendBroadcast(String s)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(s);
                for (int i = defaultPort; i <= sendPortMax; i++)
                    sender.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Broadcast, i));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler(new eventArgs(null, "err", "250" + e.ToString()));
            }
        }
    }
}
