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
using Bank_ClientApp;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bank_ClientApp
{
    /// 
    /// * - you will not be able to test application as client-diferent. GenerateClientID() will use your Processor ID to generate KEY file on server.
    /// 
    public partial class MainWindow : Window
    {
        //
        private IPAddress debug_ServerIP = IPAddress.Loopback; //Change this to IP you use.

        //
        private string UNIQ_ID = ClientManager.GenerateFakeClientID(); //Change to GenerateClientID(), if you want use ProcessorID Identificaton*.

        //Socket data
        private Socket ClientSocket;
        private const int PORT = 100;
        private bool waitingConnection = false;
        public bool isConnected = false;


        public MainWindow() //Enter method. 
        {
            InitializeComponent();
        }

        private void ConnectToServer(bool restoreObjects) //Main connection method. If you want to connect, please use this. Fully safe method. 
        {
            if (!waitingConnection && restoreObjects)
            {
                firstLogoImage.BeginAnimation(Image.OpacityProperty, null);
                firstLogoImage.Opacity = 1.0d;
                ChangeUIState(WindowUIStates.Logo);

                ConnectToServerAsync();
            }
            else if (!waitingConnection && !restoreObjects)
                ConnectToServerAsync();
        }

        private async void ConnectToServerAsync() //Async method to connect to server. 
        {
            connectionStatusLabel.Visibility = Visibility.Visible;
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await Task.Run(() => { SocketConnectToServer(); });

            if (ClientSocket.Connected)
            {
                isConnected = true;
                firstLogoImage.BeginAnimation(Image.OpacityProperty, null);
                firstLogoImage.Opacity = 0.0d;
                ChangeUIState(WindowUIStates.LogIn);
                connectionStatusLabel.Visibility = Visibility.Hidden;
                DetectConnectionLost();
            }
            else
            {
                isConnected = false;
                connectionStatusLabel.Content = "No connection! Retrying...";
                ConnectToServer(true);
            }
        }

        private async void DetectConnectionLost() //Don't use this method at all! It is async in background! If you want to stop it - change 'while (true)' to 'while (false)'. 
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        SendRequestToServer(0);
                    }
                    catch (SocketException)
                    {
                        break;
                    }
            };
            });
            isConnected = false;
            connectionStatusLabel.Content = "Lost connection! Retrying...";
            ConnectToServer(true);
        }

        /// REQUESTS LIST:
        /// 
        /// 0 - Reserved request! Please, don't use it!
        /// 1 - Check login data in clients database on server.
        /// 2 - Register new account
        /// 100 - Close connection to server.
        /// 101 - Not a request! It closes connection without end-request, so it isn't using connection.
        ///
        public void SendRequestToServer(int requestType) //Send specific request to server if connected. Unsafe method. 
        {
            if (requestType == 0)
                SocketSendStringRequest(Encryptor.EncryptString("c_check"));
            if (requestType == 1)
                SocketSendStringRequest(Encryptor.EncryptString("ldc_" + loginBox.Text + "_" + passwordBox.Text + "_" + UNIQ_ID)); //login data check 
            if (requestType == 2)
                SocketSendStringRequest(Encryptor.EncryptString("nclient_" + signUp_LoginBox.Text + "_" + signUp_PasswordBox.Text + "_" + signUp_EmailBox.Text + "_" + signUp_PhoneBox.Text + "_" + UNIQ_ID));
            if (requestType == 100)
                SocketCloseConnectionToServer();
            if (requestType == 101)
                SocketCloseConnectionToServerSilent();
        }

        private void SocketSendStringRequest(string text) //Socket method to put input string to byte array and send this array to server if connected. Unsafe method. 
        {
            byte[] buffer = Encoding.Unicode.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private void SocketConnectToServer() //Socket method to try to connect to server. 
        {
            waitingConnection = true;
            int attempts = 0;
            while (!ClientSocket.Connected && attempts < 5)
            {
                try
                {
                    ClientSocket.Connect(debug_ServerIP, PORT);
                }
                catch (SocketException)
                {
                    attempts++;
                }
            }
            waitingConnection = false;
        }

        private void SocketCloseConnectionToServer() //Socket method to close connection to the server with end-request. 
        {
            SocketSendStringRequest("client_close_connection"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }

        private void SocketCloseConnectionToServerSilent() //Socket method to close connection to the server with NO end-request. 
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }

        public string SocketReceivedResponse() //Socket method to write socket data to bite array and then convert to string. Unsafe method. 
        {
            var buffer = new byte[2048];
            int numberOfReceivedBytes = ClientSocket.Receive(buffer);
            if (numberOfReceivedBytes == 0)
                return "";
            var receivedData = new byte[numberOfReceivedBytes]; //optimize array
            Array.Copy(buffer, receivedData, numberOfReceivedBytes);
            string receivedText = Encoding.Unicode.GetString(receivedData);
            return Encryptor.DecryptString(receivedText);
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e) //Login button click event. 
        {
            if (loginBox.Text == string.Empty || passwordBox.Text == string.Empty)
            {
                wrongDataLabel.Visibility = Visibility.Visible;
            }
            else
            {
                wrongDataLabel.Visibility = Visibility.Hidden;
                try
                {
                    SendRequestToServer(1);
                    string answer = SocketReceivedResponse();
                    if (answer != "acc")
                        wrongDataLabel.Visibility = Visibility.Visible;
                    else
                    {
                        CIWindow ciw = new CIWindow();
                        ciw.myHandler = this;
                        ciw.Show();
                        this.ShowInTaskbar = false;
                        this.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception) { }
            }
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e) //Go to registration state button click event. 
        {
            ChangeUIState(WindowUIStates.SignUp);
        }

        private void SignUp_BackButton_Click(object sender, RoutedEventArgs e) //Go back to login state button click event. 
        {
            ChangeUIState(WindowUIStates.LogIn);
        }

        private void SignUp_SignUpButton_Click(object sender, RoutedEventArgs e) //Registration button click event. All requests to register are here. 
        {
            if (signUp_PasswordBox.Text == signUp_ConfirmPasswordBox.Text && 
                CheckEmailCorrection(signUp_PasswordBox.Text) && 
                signUp_PhoneBox.Text.All(Char.IsDigit) && !signUp_PasswordBox.Text.Contains("@") && !signUp_LoginBox.Text.Contains("@"))
            {
                signUp_WrongDataLabel.Visibility = Visibility.Hidden;
                try
                {
                    SendRequestToServer(2); string answer = SocketReceivedResponse();
                    if (answer != "acc")
                        signUp_WrongDataLabel.Visibility = Visibility.Visible;
                    else
                        MessageBox.Show("Done!");
                }
                catch (Exception) { }
            }
            else
                signUp_WrongDataLabel.Visibility = Visibility.Visible;
        }

        private bool CheckEmailCorrection(string text) //Check email for true adress. 
        {
            if (text.Contains("@gmail.com") || text.Contains("@i.ua") || text.Contains("@ua.fm") ||
                text.Contains("@email.ua") || text.Contains("@mail.ru") || text.Contains("@inbox.ru") ||
                text.Contains("@list.ru") || text.Contains("@bk.ru") || text.Contains("@cbn.net.id") ||
                text.Contains("@yahoo.com") || text.Contains("@hotmail.com") || text.Contains("@meta.ua") ||
                text.Contains("@eesti.ee") || text.Contains("@hot.ee") || text.Contains("@yandex.ru"))
                return false;
            else
                return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) //Loaded method. When window is opened - it will call. 
        {
            firstLogoStoryBoard.Completed += new EventHandler(FirstLogoAnimationCompleted);
            ChangeUIState(WindowUIStates.Logo);
        }

        private void FirstLogoAnimationCompleted(object sender, EventArgs e) //Logo animation end. We bind new animation with null refenrence to be able to change Opacity property in C#, otherwise we couldn't. 
        {
            firstLogoImage.BeginAnimation(Image.OpacityProperty, null);
            firstLogoImage.Opacity = 0.0d;
            ConnectToServer(false);
        }

        private void ChangeUIState(WindowUIStates wuis) //Change UI Visibility property. 
        {
            if (wuis == WindowUIStates.Empty)
            {
                logInGrid.Visibility = Visibility.Hidden;
                signUpGrid.Visibility = Visibility.Hidden;
                firstLogoImage.Visibility = Visibility.Hidden;
            }
            if (wuis == WindowUIStates.LogIn)
            {
                logInGrid.Visibility = Visibility.Visible;
                signUpGrid.Visibility = Visibility.Hidden;
                firstLogoImage.Visibility = Visibility.Hidden;
            }
            if (wuis == WindowUIStates.SignUp)
            {
                logInGrid.Visibility = Visibility.Hidden;
                signUpGrid.Visibility = Visibility.Visible;
                firstLogoImage.Visibility = Visibility.Hidden;
            }
            if (wuis == WindowUIStates.Logo)
            {
                logInGrid.Visibility = Visibility.Hidden;
                signUpGrid.Visibility = Visibility.Hidden;
                firstLogoImage.Visibility = Visibility.Visible;
            }

            //switch (wuis)
            //{
            //    case WindowUIStates.Empty:
            //        logInGrid.Visibility = Visibility.Hidden;
            //        signUpGrid.Visibility = Visibility.Hidden;
            //        firstLogoImage.Visibility = Visibility.Hidden;
            //        break;

            //    case WindowUIStates.LogIn:
            //        logInGrid.Visibility = Visibility.Visible;
            //        signUpGrid.Visibility = Visibility.Hidden;
            //        firstLogoImage.Visibility = Visibility.Hidden;
            //        break;

            //    case WindowUIStates.SignUp:
            //        logInGrid.Visibility = Visibility.Hidden;
            //        signUpGrid.Visibility = Visibility.Visible;
            //        firstLogoImage.Visibility = Visibility.Hidden;
            //        break;

            //    case WindowUIStates.Logo:
            //        logInGrid.Visibility = Visibility.Hidden;
            //        signUpGrid.Visibility = Visibility.Hidden;
            //        firstLogoImage.Visibility = Visibility.Visible;
            //        break;
            //}

        }

        enum WindowUIStates //UI States: 'Empty', 'LogIn', 'SignUp', 'Logo' 
        {
            Logo,
            LogIn,
            SignUp,
            Empty
        }
    }
}
