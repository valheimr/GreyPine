using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace Bank_Server
{
    class Program
    {
        //Sockets data
        private static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static List<Socket> clientSockets = new List<Socket>();
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;



        //Directory to clients data on server.
        private static string clientsDirectoryPath;

        //Log flags
        private static bool Flag_FullErrors = false;
        private static bool Flag_LogMessageDate = true;



        static void Main() //Enter method 
        {

            Process[] lprcTestApp = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if (lprcTestApp.Length > 1)
            {
                Console.WriteLine("Server is already run!");
                Console.ReadLine();
                return;
            }

            //AppDomain.CurrentDomain.ProcessExit += new EventHandler(AppExit);

            CreateOrUpdateLogFile();

            Console.Title = "Grey Pine Main Server";

            SetupServer();

            Log("Server Started!", false, LogStates.Program, Flag_LogMessageDate);
            Log("Type 'help' to show commands list.", true, LogStates.ConsoleText, Flag_LogMessageDate);

            while (true)
            {
                string txt = Console.ReadLine().ToString();
                if (txt == "cl")
                    CloseAllSockets();
                else if (txt == "fur")
                {
                    Log("fur", false, LogStates.Command, Flag_LogMessageDate);
                    Flag_FullErrors = true;
                    Log("Flag_FullErrors changed to true", true, LogStates.Program, Flag_LogMessageDate);
                }
                else if (txt == "shr")
                {
                    Log("shr", false, LogStates.Command, Flag_LogMessageDate);
                    Flag_FullErrors = false;
                    Log("Flag_FullErrors changed to false", true, LogStates.Program, Flag_LogMessageDate);
                }
                else if (txt == "shwcl")
                {
                    Log("shwcl", false, LogStates.Command, Flag_LogMessageDate);
                    if (clientSockets.Count != 0)
                        foreach (var a in clientSockets)
                        {
                            Console.WriteLine(a.RemoteEndPoint);
                        }
                    else
                        Console.WriteLine("No clients!");
                }
                else if (txt == "clr")
                {
                    Console.Clear();
                    Log("clr", false, LogStates.Command, Flag_LogMessageDate);
                }
                else if (txt == "lgdt")
                {
                    Flag_LogMessageDate = true;
                    Log("Flag_LogMessageDate changed to true", true, LogStates.Program, true);
                }
                else if (txt == "lgndt")
                {
                    Flag_LogMessageDate = false;
                    Log("Flag_LogMessageDate changed to false", true, LogStates.Program, false);
                }
                else if (txt == "rstr")
                {
                    Log("rstr", false, LogStates.Command, Flag_LogMessageDate);
                    RestartServer();
                }
                else if (txt.Contains("rstr") && txt.Length > 5)
                {
                    string[] a = txt.Split(' ');
                    Log("rstr", false, LogStates.Command, Flag_LogMessageDate);
                    try
                    {
                        RestartServer(int.Parse(a[1]));
                    }
                    catch(Exception)
                    {
                        RestartServer();
                    }
                }
                else if (txt == "help")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("cl - close server");
                    Console.WriteLine("fur - show full errors");
                    Console.WriteLine("shr - show short errors");
                    Console.WriteLine("shwcl - show client's IPs");
                    Console.WriteLine("rstr - restart server");
                    Console.WriteLine("clr - clear console");
                    Console.WriteLine("lgdt - write time to log");
                    Console.WriteLine("lgndt - do not write time to log");
                    Console.ForegroundColor = ConsoleColor.White;
                    Log("help", false, LogStates.Command, Flag_LogMessageDate);
                }
            }
        }

        private static void SendSocketData(string data_txt, Socket currentSocket) //Send data to client. 
        {
            byte[] data = Encoding.Unicode.GetBytes(Encryptor.EncryptString(data_txt));
            currentSocket.Send(data);
        }


        /// 
        /// Server methods
        /// All methods below are only for interserver logic 
        /// 


        private static void SetupServer() //Special method to setup server on machine. 
        {
            Console.WriteLine("Setting up server...");

            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "ClientsData")))
            {
                Console.WriteLine("Can't find Cients Data directory! Creating new...");
                try
                {
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ClientsData"));
                    clientsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "ClientsData");
                    Console.WriteLine("Created.");
                    Log("Created directory - " + clientsDirectoryPath, false, LogStates.Program, Flag_LogMessageDate);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Can't create directory Clients Data! See error below:");
                    if (Flag_FullErrors)
                        Console.WriteLine(e.ToString());
                    else
                        Console.WriteLine("Error in directory creation.");
                    Log("Failed to create directory - " + Path.Combine(Directory.GetCurrentDirectory(), "ClientsData"), false, LogStates.Program, Flag_LogMessageDate);
                    return;
                }
            }
            else
            {
                clientsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "ClientsData");
                Console.WriteLine("Clients Data directory is found.");
                Log("Clients data found in directory - " + clientsDirectoryPath, false, LogStates.Program, Flag_LogMessageDate);
            }

            try
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
                serverSocket.Listen(0);
                serverSocket.BeginAccept(AcceptCallback, null);
                Log("Successfully binded socket to IP", false, LogStates.Program, Flag_LogMessageDate);
            }
            catch (Exception e)
            {
                if (Flag_FullErrors)
                    Console.WriteLine(e.ToString());
                Log("Failed to bind socket to IP", false, LogStates.Program, Flag_LogMessageDate);
                Log(e.ToString(), false, LogStates.Error, Flag_LogMessageDate);
                Console.WriteLine("Failed to create server! See log file in Logs folder!");
            }
            Console.WriteLine("Server setup complete.");
        } 

        private static void CloseAllSockets() //Close all sockets and break connection. 
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Log("Connection to socket closed", false, LogStates.Program, Flag_LogMessageDate);
            }
            serverSocket.Close();
            Log("Server socket closed", true, LogStates.Program, Flag_LogMessageDate);
        }

        private async static void RestartServer() //Restrat server without delays by reidentifing serverSocket and setup again. 
        {
            await Task.Run(() => {
                try { CloseAllSockets(); }
                catch (Exception) { }
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            });
            Log("Restrat server", false, LogStates.Program, Flag_LogMessageDate);
            SetupServer();
        }

        private async static void RestartServer(int sleepTime) //Same as RestartServer() but with delay (about 1000-5000 for tests). 
        {
            await Task.Run(() => {
                try { CloseAllSockets(); }
                catch (Exception) { }
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            });
            Log("Restrat server in " + sleepTime.ToString() + "ms", false, LogStates.Program, Flag_LogMessageDate);
            Thread.Sleep(sleepTime);
            SetupServer();
        }

        private static void AcceptCallback(IAsyncResult AR) //Async delegated method to accept connection and link ReceiveCallback() method. 
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (Exception e)
            {
                if (Flag_FullErrors)
                    Console.WriteLine(e.ToString());
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("New Connection detected!");
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR) //Async delegated method to get requests from client sockets and transform them to string. 
        {
            Socket currentSocket = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = currentSocket.EndReceive(AR);
            }
            catch (Exception e)
            {
                Console.WriteLine("Wrong client side! See error below:");
                currentSocket.Close();
                clientSockets.Remove(currentSocket);
                if (!Flag_FullErrors)
                    Console.WriteLine("Client forcely disconnected.");
                else
                    Console.WriteLine(e.ToString());
                Log("Failed to receive callback!", false, LogStates.Program, Flag_LogMessageDate);
                Log(e.ToString(), false, LogStates.Error, Flag_LogMessageDate);
                return;
            }
            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encryptor.DecryptString(Encoding.Unicode.GetString(recBuf));

            if (text != "c_check")
                Log("Received callback - " + Encryptor.EncryptString(text), false, LogStates.Program, Flag_LogMessageDate);

            CheckRequestType(text, currentSocket);

            currentSocket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentSocket);
        }

        private static void CheckRequestType(string text, Socket currentSocket) //Check string from ReceiveCallback() method and identified request. 
        {
            if (text.Contains("ldc"))
            {
                Log("Someone is trying to login...", false, LogStates.Program, Flag_LogMessageDate);
                if (CheckLoginData(text, currentSocket))
                    Log("Successully!", false, LogStates.Program, Flag_LogMessageDate);
                else
                    Log("Failed!", false, LogStates.Program, Flag_LogMessageDate);
            }
            else if (text.Contains("nclient"))
            {
                Log("Someone is trying to register...", false, LogStates.Program, Flag_LogMessageDate);
                if (CheckRegistrationData(text, currentSocket))
                    Log("Successully!", false, LogStates.Program, Flag_LogMessageDate);
                else
                    Log("Failed!", false, LogStates.Program, Flag_LogMessageDate);
            }
            else if (text.ToLower() == "client_close_connection")
            {
                currentSocket.Shutdown(SocketShutdown.Both);
                currentSocket.Close();
                clientSockets.Remove(currentSocket);
                Console.WriteLine("Client disconnected");
                return;
            }
            else if (text.ToLower() == "c_check")
            { }
            else
                Console.WriteLine("Invalid request");
        }

        private static bool CheckRegistrationData(string text, Socket currentSocket) //Check for registration accessibility. 
        {
            string[] arr = text.Split('_');
            string login = Encryptor.EncryptString(arr[1]);
            string password = Encryptor.EncryptString(arr[2]);
            string email = Encryptor.EncryptString(arr[3]);
            string phone = Encryptor.EncryptString(arr[4]);
            string uniqid = arr[5];
            if (File.Exists(@"ClientsData\" + uniqid + ".ld"))
            {
                string[] pth = Directory.GetFiles(clientsDirectoryPath);
                int count = 0;
                foreach (var currentPath in pth)
                {
                    count++;
                    if (File.ReadAllText(currentPath).Contains(login))
                    {
                        SendSocketData("err_lgn", currentSocket);
                        Log("Failed to register new user!", false, LogStates.Program, Flag_LogMessageDate);
                        return false;
                    }
                    else if (File.ReadAllText(currentPath).Contains(email))
                    {
                        SendSocketData("err_eml", currentSocket);
                        Log("Failed to register new user!", false, LogStates.Program, Flag_LogMessageDate);
                        return false;
                    }
                    else if (File.ReadAllText(currentPath).Contains(phone))
                    {
                        SendSocketData("err_phn", currentSocket);
                        Log("Failed to register new user!", false, LogStates.Program, Flag_LogMessageDate);
                        return false;
                    }
                }
                File.Create(@"ClientsData\" + uniqid + "_" + count.ToString() + ".ld").Close();
                Log("New file created: " + uniqid + "_" + count.ToString() + ".ld", false, LogStates.Program, Flag_LogMessageDate);
                File.AppendAllText(@"ClientsData\" + uniqid + "_" + count.ToString() + ".ld", BuildRegistrationStringDecrypted(login, password, email, phone));
                Log("Appended text to file " + uniqid + "_" + count.ToString() + ".ld" + ": " + BuildRegistrationStringDecrypted(login, password, email, phone), false, LogStates.Program, Flag_LogMessageDate);
                byte[] data_2 = Encoding.Unicode.GetBytes(Encryptor.EncryptString("acc"));
                currentSocket.Send(data_2);
                return true;
            }
            else
            {
                string[] pth = Directory.GetFiles(clientsDirectoryPath);
                foreach (var currentPath in pth)
                {
                    if (File.ReadAllText(currentPath).Contains(login))
                    {
                        SendSocketData("err_lgn", currentSocket);
                        Log("Failed to register new user!", false, LogStates.Program, Flag_LogMessageDate);
                        return false;
                    }
                    else if (File.ReadAllText(currentPath).Contains(email))
                    {
                        SendSocketData("err_eml", currentSocket);
                        Log("Failed to register new user!", false, LogStates.Program, Flag_LogMessageDate);
                        return false;
                    }
                    else if (File.ReadAllText(currentPath).Contains(phone))
                    {
                        SendSocketData("err_phn", currentSocket);
                        Log("Failed to register new user!", false, LogStates.Program, Flag_LogMessageDate);
                        return false;
                    }
                }
                File.Create(@"ClientsData\" + uniqid + ".ld").Close();
                Log("New file created: " + uniqid + ".ld", false, LogStates.Program, Flag_LogMessageDate);
                File.AppendAllText(@"ClientsData\" + uniqid + ".ld", BuildRegistrationStringDecrypted(login, password, email, phone));
                Log("Appended text to file " + uniqid + ".ld" + ": " + BuildRegistrationStringDecrypted(login, password, email, phone), false, LogStates.Program, Flag_LogMessageDate);
                byte[] data_2 = Encoding.Unicode.GetBytes(Encryptor.EncryptString("acc"));
                currentSocket.Send(data_2);
                return true;
            }
        }

        private static bool CheckLoginData(string text, Socket currentSocket) //Check login data for conformity. 
        {
            string[] arr = text.Split('_');
            string login = Encryptor.EncryptString(arr[1]);
            string password = Encryptor.EncryptString(arr[2]);
            string uniqid = arr[3];
            string[] pth = Directory.GetFiles(clientsDirectoryPath);
            foreach (var currentPath in pth)
            {
                if (File.ReadAllText(currentPath).Contains(login) && File.ReadAllText(currentPath).Contains(password))
                {
                    SendSocketData("acc", currentSocket);
                    Log("Found user: " + Path.GetFileName(currentPath), false, LogStates.ConsoleText, Flag_LogMessageDate);
                    return true;
                }
            }
            SendSocketData("err_lgin", currentSocket);
            Log("Can't find user: " + Path.GetFileName(login), false, LogStates.ConsoleText, Flag_LogMessageDate);
            return false;
        }

        private static string BuildRegistrationStringDecrypted(string login, string pass, string mail, string phone) //Build string for writing it to file. Be carefull, it is decrypted! 
        {
            return "Login: " + login + "\n" + "Password: " + pass + "\n" + "E-mail: " + mail + "\n" + "Phone: " + phone + "\n";
        }

        private static void Log(string logText, bool logConsole, LogStates ls, bool logTime) //Log method. Can replace Console.WriteLine() but it is more complex. 
        {
            if (logConsole)
                Console.WriteLine(logText);
            try
            {
                if (ls == LogStates.ConsoleText && !logTime)
                    File.AppendAllText(@"Logs\_log.txt", "Console message: " + logText + "\n");
                else if (ls == LogStates.Error && !logTime)
                    File.AppendAllText(@"Logs\_log.txt", "Error: " + logText + "\n");
                else if (ls == LogStates.Command && !logTime)
                    File.AppendAllText(@"Logs\_log.txt", "Command: " + logText + "\n");
                else if (ls == LogStates.Program && !logTime)
                    File.AppendAllText(@"Logs\_log.txt", "Program: " + logText + "\n");
                else if (ls == LogStates.ConsoleText && logTime)
                    File.AppendAllText(@"Logs\_log.txt", "[" + DateTime.Now.ToString() + "]" + "Console message: " + logText + "\n");
                else if (ls == LogStates.Error && logTime)
                    File.AppendAllText(@"Logs\_log.txt", "[" + DateTime.Now.ToString() + "]" + "Error: " + logText + "\n");
                else if (ls == LogStates.Command && logTime)
                    File.AppendAllText(@"Logs\_log.txt", "[" + DateTime.Now.ToString() + "]" + "Command: " + logText + "\n");
                else if (ls == LogStates.Program && logTime)
                    File.AppendAllText(@"Logs\_log.txt", "[" + DateTime.Now.ToString() + "]" + "Program: " + logText + "\n");
            }
            catch (Exception) { }
        }

        private static void CreateOrUpdateLogFile() //Create or repair Log file in 'Logs' folder. 
        {
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Logs")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
                File.Create(@"Logs\_log.txt").Close();
                File.AppendAllText(@"Logs\_log.txt", "\n \n[" + DateTime.Now.ToString() + "]" + "\n");
                File.AppendAllText(@"Logs\_log.txt", "Log started!" + "\n");
            }
            else if (!File.Exists(@"Logs\_log.txt"))
            {
                File.Create(@"Logs\_log.txt").Close();
                File.AppendAllText(@"Logs\_log.txt", "\n \n[" + DateTime.Now.ToString() + "]" + "\n");
                File.AppendAllText(@"Logs\_log.txt", "Log started!" + "\n");
            }
            else
            {
                File.AppendAllText(@"Logs\_log.txt", "\n \n[" + DateTime.Now.ToString() + "]" + "\n");
                File.AppendAllText(@"Logs\_log.txt", "Log started!" + "\n");
            }

        }

        private enum LogStates //Enum for log states: 'Console', 'Error', 'Command', 'Program'. 
        {
            ConsoleText,
            Error,
            Command,
            Program
        }
    }
}
