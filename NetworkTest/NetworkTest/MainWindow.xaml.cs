using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetworkTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UDP udp;
        StringBuilder sb=new StringBuilder();
        bool login = false;
        TCPServer server;
        TCPClient client;
        public MainWindow()
        {
            InitializeComponent();
            udp = new UDP();
            
        }
        void onReceive(UDP.eventArgs args)
        {
            if(args.token=="conn")
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        sb.AppendLine(args.client.name + " connected.");
                        TB_Chats.Text = sb.ToString();
                    }));
                }).Start();
                
            }
            if (args.token == "msg")
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        sb.AppendLine(args.client.name + ": " + args.msg);
                        TB_Chats.Text = sb.ToString();
                    }));
                }).Start();

            }
            if (args.token == "init")
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Title = "R: "+args.msg;
                    }));
                }).Start();
            }
            if (args.token == "connacc")
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Title += " Find Server!";
                        Console.WriteLine("Find Server!");
                        sb.AppendLine("Find Server!");
                        TB_Chats.Text = sb.ToString();
                    }));
                }).Start();
            }
            if (args.token == "disconn")
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        Console.WriteLine(args.msg+" disconnect from server!");
                        sb.AppendLine(args.msg + " disconnect from server!");
                        TB_Chats.Text = sb.ToString();
                    }));
                }).Start();
            }
            if (args.token == "err")
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        sb.AppendLine("error: "+args.msg);
                        TB_Chats.Text = sb.ToString();
                    }));
                }).Start();
            }
        }
        private void BTN_Login_Click(object sender, RoutedEventArgs e)
        {
            if (!login)
            {
                //foreach (IPAddress IP in Dns.GetHostAddresses(Dns.GetHostName()))
                //{
                //    if(IP.AddressFamily==AddressFamily.InterNetwork)
                //        sb.AppendLine(IP.AddressFamily + ", " + IP.IsIPv6LinkLocal + ", " + IP.ToString());
                //}
                BTN_Login.IsEnabled = false;
                UDP.handler += onReceive;
                udp.Init();
                udp.Receive();
                udp.GetSendPort();
                Thread.Sleep(1000);
                //should put into work thread
                udp.SearchServer(TB_Name.Text);
                if (udp.findServer)
                {
                    sb.AppendLine("Find Server!");
                    client = new TCPClient();
                    client.Init(udp.tcpServer);
                }
                else
                {
                    
                    server = new TCPServer();
                    server.Init();
                    udp.tcpServer = TCPServer.serverIEP;
                    sb.AppendLine("No Server Found! Est TCP Server: " + TCPServer.serverIEP.ToString());
                }
                login = true;
                BTN_Login.Content = "Logout";
                BTN_Login.IsEnabled = true;
                TB_Chats.Text = sb.ToString();
            }
            else if (login)
            {
                BTN_Login.IsEnabled = false;
                
                udp.SendBroadcast("disconn|" + TB_Name.Text);
                sb.Clear();
                TB_Chats.Text = sb.ToString();
                UDP.handler -= onReceive;
                udp.Close();
                if(server!=null)
                    server.Close();
                if (client != null)
                    client.Close();
                login = false;
                BTN_Login.Content = "Login";
                BTN_Login.IsEnabled = true;
            }
        }

        private void BTN_Send_Click(object sender, RoutedEventArgs e)
        {
            SendMsg();
        }

        private void WIN_Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (server != null)
                    server.Close();
                if (client != null)
                    client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void TB_Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMsg();
            }
        }
        private void SendMsg()
        {
            sb.AppendLine("You: " + TB_Input.Text);
            udp.SendBroadcast("msg|" + TB_Input.Text);
            TB_Input.Text = "";
            TB_Chats.Text = sb.ToString();
        }
        private void BTN_P2P_Click(object sender, RoutedEventArgs e)
        {
            //foreach (IPAddress IP in Dns.GetHostAddresses(Dns.GetHostName()))
            //{
            //    if (IP.AddressFamily == AddressFamily.InterNetwork)
            //        sb.AppendLine(IP.AddressFamily + ", " + IP.IsIPv6LinkLocal + ", " + IP.ToString());
            //}
            BTN_Login.IsEnabled = false;
            UDP.handler += onReceive;
            udp.Init();
            udp.Receive();
            udp.GetSendPort();
            Thread.Sleep(1000);
            //should put into work thread
            udp.P2PSearch(TB_Name.Text);
            if (udp.findServer)
            {
                sb.AppendLine("Find Server!");
                client = new TCPClient();
                client.Init(udp.tcpServer);
            }
            else
            {
                sb.AppendLine("No Server Found!");
                server = new TCPServer();
                server.Init();
                udp.tcpServer = TCPServer.serverIEP;
            }
        }
    }
    
    public class Client
    {
        public IPEndPoint iep;
        public string name;
        public Client(IPEndPoint ep,string n)
        {
            iep = ep;
            name = n;
        }
    }
}
