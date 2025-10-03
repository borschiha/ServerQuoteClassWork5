using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks; // Требуется для async/await и Task

namespace Server_cl5_Async
{
    internal class Program
    {
        private const int _port = 1234;
        private const string _serverIp = "10.2.206.67";
        private const string _appname = "cl5ServerAsync";

        private static readonly Random _random = new Random();

        private static readonly List<string> AllEnglishQuotes = new List<string>
        {
            "You are my today and all of my tomorrows. – Leo Christopher.",
            "Together is my favourite place to be.",
            "Love doesn't make the world go round. Love is what makes the ride worthwhile. – Franklin P. Jones.",
            "Every love story is beautiful, but ours is my favourite.",
            "Stay close to people who feel like sunshine.",

            "I'm not lazy; I'm just on energy-saving mode.",
            "Some people graduate with honors; I am just honored to graduate.",
            "I'm not arguing. I'm just explaining why I'm right.",
            "If Monday had a face, I would punch it.",
            "I love being married. It's so great to find that one special person you want to annoy for the rest of your life. – Rita Rudner.",
            "Clothes make the man. Naked people have little or no influence in society. – Mark Twain.",
            "I love you more than pizza. And that's saying something.",
            "I'm sick of following my dreams, man. I'm just going to ask where they're going and hook up with 'em later. – Mitch Hedberg.",
            "When your mother asks, 'Do you want a piece of advice?' it is a mere formality. It doesn't matter if you answer yes or no. You're going to get it anyway. – Erma Bombeck.",
            "I'm not short; I'm fun-sized!",

            "Dream big, work hard, and stay humble.",
            "The best way to predict your future is to create it. – Peter Drucker.",
            "Success is not final, failure is not fatal: It is the courage to continue that counts. – Winston Churchill.",
            "Happiness is not something ready-made. It comes from your own actions. – Dalai Lama.",
            "Do more of what makes you happy.",
            "Every day is a second chance.",
            "When nothing goes right, go left.",
            "Smile - it's the key that fits the lock of everybody's heart.",
            "Life's too short to wear boring socks.",
            "Coffee in one hand, phone in the other."
        };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            AddFirewallRule(_port, _appname);

            TcpListener listener = null;
            try
            {
                IPAddress ip = IPAddress.Parse(_serverIp);
                IPEndPoint ep = new IPEndPoint(ip, _port);

                listener = new TcpListener(ep);

                listener.Start();
                Console.WriteLine($"*** SERVER START WORKING on {_serverIp}:{_port} ***");
                Console.WriteLine("Server is ready to accept multiple clients...");

                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (SocketException ex) when (ex.ErrorCode == 10004)
            {
                Console.WriteLine("Listener stopped gracefully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server fatal error: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
                Console.WriteLine("*** SERVER SHUT DOWN ***");
            }
        }
        private static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                string clientAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                int clientPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
                DateTime connectTime = DateTime.Now;

                Console.WriteLine($"*** CLIENT CONNECTED ***");
                Console.WriteLine($"Client Info: {clientAddress}:{clientPort} | Connect Time: {connectTime}");

                using (NetworkStream stream = client.GetStream())
                {
                    try
                    {
                        byte[] buf = new byte[1024];
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buf, 0, buf.Length)) > 0)
                        {
                            string message = Encoding.UTF8.GetString(buf, 0, bytesRead).Trim().ToLower();
                            Console.WriteLine($"Received from {clientAddress}: '{message}'");

                            if (message == "quote")
                            {
                                string response = GetRandomQuote();
                                byte[] bytes = Encoding.UTF8.GetBytes(response);
                                await stream.WriteAsync(bytes, 0, bytes.Length);
                                Console.WriteLine($"Sent quote to {clientAddress}.");
                            }
                            else
                            {
                                string response = $"Server received your message: '{message}'. Send 'quote' for a quote.";
                                byte[] bytes = Encoding.UTF8.GetBytes(response);
                                await stream.WriteAsync(bytes, 0, bytes.Length);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Communication error with {clientAddress}: {ex.Message}");
                    }
                }

                DateTime disconnectTime = DateTime.Now;
                Console.WriteLine($"*** CLIENT DISCONNECTED: {clientAddress} | Disconnect Time: {disconnectTime} ***");
            }
        }

        public static string GetRandomQuote()
        {
            int randomIndex = _random.Next(AllEnglishQuotes.Count);
            return AllEnglishQuotes[randomIndex];
        }

        public static void AddFirewallRule(int port, string appName)
        {
            try
            {
                string deleteArgs = $"advfirewall firewall delete rule name=\"{appName}\"";
                Process.Start("netsh", deleteArgs)?.WaitForExit();

                string addArgs = $"advfirewall firewall add rule name=\"{appName}\" dir=in action=allow protocol=TCP localport={port}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = addArgs,
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
                Console.WriteLine($"Firewall rule '{appName}' requested. Please confirm UAC prompt.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add firewall rule: {ex.Message}");
            }
        }
    }
}