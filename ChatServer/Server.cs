using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Net;
using System.Collections;

using ChatApplication;
using System.Threading;

namespace ChatServer
{
    public partial class Server : Form
    {
        #region Private Members

        

        // Structure to store the client information
        private struct Client
        {
            public EndPoint endPoint;
            public string name;
            public string Senha;
            public string IP;
            public string Porta;
            public bool Online;
            public int Id;
        }

        // Listing of clients
        private ArrayList clientList;

        // Server socket
        private Socket serverSocket;

        // Data stream
        private byte[] dataStream = new byte[1024];

        // Status delegate
        private delegate void UpdateStatusDelegate(string status);
        private UpdateStatusDelegate updateStatusDelegate = null;

        #endregion

        #region Constructor

        public Server()
        {
            InitializeComponent();
        }

        #endregion

        #region Events

        private void Server_Load(object sender, EventArgs e)
        {
            try
            {
                // Initialise the ArrayList of connected clients
                this.clientList = new ArrayList();

                // Initialise the delegate which updates the status
                this.updateStatusDelegate = new UpdateStatusDelegate(this.UpdateStatus);

                // Initialise the socket
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Initialise the IPEndPoint for the server and listen on port 30000
                IPEndPoint server = new IPEndPoint(IPAddress.Any, 30000);

                // Associate the socket with this IP address and port
                serverSocket.Bind(server);

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = (EndPoint)clients;

                // Start listening for incoming data
                serverSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);

                lblStatus.Text = "Listening";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error";
                MessageBox.Show("Load Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region Send And Receive

        public void SendData(IAsyncResult asyncResult)
        {
            try
            {
                serverSocket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendData Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReceiveData(IAsyncResult asyncResult)
        {
            try
            {
                bool dontsend = false;
                byte[] data;

                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(this.dataStream);

                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = (EndPoint)clients;

                // Receive all data
                serverSocket.EndReceiveFrom(asyncResult, ref epSender);

                // Start populating the packet to be sent
                sendData.ReadData.Add("ChatDataIdentifier", receivedData.ReadData["ChatDataIdentifier"]);
                sendData.ReadData.Add("ChatName", receivedData.ReadData["ChatName"]);
                sendData.ReadData.Add("ChatIp", epSender.ToString());
                DataIdentifier ChatDataIdentifier = receivedData.GetDataIdentifier;
                switch (ChatDataIdentifier)
                {
                    case DataIdentifier.Message:
                        sendData.ReadData["ChatMessage"] = string.Format("{0}: {1}", receivedData.ReadData["ChatName"], receivedData.ReadData["ChatMessage"]);
                        break;

                    case DataIdentifier.LogIn:
                        // Populate client object
                        Client client = new Client();
                        client.endPoint = epSender;
                        client.name = receivedData.ReadData["ChatName"] as string;
                        client.Senha = receivedData.ReadData["ChatPassword"] as string;
                        client.IP = client.endPoint.ToString();
                        client.Id = receivedData.GetInt("ChatId");
                        client.Online = true;

                        sendData.ReadData.Add("ChatId", client.Id);

                        int i = 0;
                        for (;i < this.clientList.Count; i++)
                        {
                            if(client.name == ((Client)this.clientList[i]).name){
                                if(((Client)this.clientList[i]).Online == false)
                                {
                                    if(((Client)this.clientList[i]).Senha == client.Senha)
                                    {
                                        //novo login
                                        sendData.ReadData["ChatMessage"] = string.Format("-- {0} is back --", receivedData.ReadData["ChatName"]);
                                        this.clientList[i] = client;
                                        sendData.ReadData["ChatDataIdentifier"] = DataIdentifier.OK;
                                        data = sendData.GetDataStream();
                                        sendData.ReadData["ChatDataIdentifier"] = DataIdentifier.LogIn;
                                        serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                                        i = this.clientList.Count + 10;
                                        RefreshListaContatosNovoUsuario(client.endPoint);
                                    }
                                    else
                                    {

                                        sendData.ReadData["ChatMessage"] = string.Format("-- {0} entered the wrong password --", receivedData.ReadData["ChatName"]);
                                        sendData.ReadData["ChatDataIdentifier"] = DataIdentifier.LogOut;
                                        data = sendData.GetDataStream();
                                        serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                                        i = this.clientList.Count + 10;
                                    }
                                }
                                else
                                {
                                    sendData.ReadData["ChatMessage"] = string.Format("-- {0} is already connected, disconnecting --", receivedData.ReadData["ChatName"]);
                                    sendData.ReadData["ChatDataIdentifier"] = DataIdentifier.LogOut;
                                    data = sendData.GetDataStream();
                                    serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                                    i = this.clientList.Count + 10;
                                }
                            }
                        }
                        if(i == this.clientList.Count)
                        {
                            // Add client to list
                            this.clientList.Add(client);

                            sendData.ReadData["ChatMessage"] = string.Format("-- {0} is online --", receivedData.ReadData["ChatName"]);
                            sendData.ReadData["ChatDataIdentifier"] = DataIdentifier.OK;
                            data = sendData.GetDataStream();
                            serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                            sendData.ReadData["ChatDataIdentifier"] = DataIdentifier.LogIn;
                            RefreshListaContatosNovoUsuario(client.endPoint);
                        }
                        break;

                    case DataIdentifier.LogOut:
                        // Remove current client from list

                        int j = 0;
                        Client cliente = new Client();
                        cliente.endPoint = epSender;
                        for (; j < this.clientList.Count; j++)
                        {
                            if (cliente.endPoint.Equals(((Client)this.clientList[j]).endPoint))
                            {
                                cliente = ((Client)this.clientList[j]);
                                cliente.Online = false;

                                this.clientList[j] = cliente;
                                sendData.ReadData["ChatIP"] = cliente.IP;
                                j = this.clientList.Count + 1;
                            }
                        }
                        sendData.ReadData["ChatMessage"] = string.Format("-- {0} has gone offline --", receivedData.ReadData["ChatName"]);
                        break;
                }

                // Get packet as byte array
                data = sendData.GetDataStream();

                foreach (Client client in this.clientList)
                {
                    if (client.endPoint != epSender /*sendData.ChatDataIdentifier != DataIdentifier.LogIn ||*/ &&  client.Online == true)
                    {
                        // Broadcast to all logged on users
                        serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                    }
                }

                // Listen for more connections again...
                serverSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epSender, new AsyncCallback(this.ReceiveData), epSender);

                // Update status through a delegate
                this.Invoke(this.updateStatusDelegate, new object[] { sendData.ReadData["ChatMessage"] });
            }
            catch (Exception ex)
            {
                MessageBox.Show("ReceiveData Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Other Methods

        private void UpdateStatus(string status)
        {
            rtxtStatus.Text += status + Environment.NewLine;
        }

        private void RefreshListaContatosNovoUsuario(EndPoint clientEndPint)
        {
            byte[] data;

            // Initialise a packet object to store the data to be sent
            Packet sendData = new Packet();
            for(int i=0; i < clientList.Count; i++)
            {
                if (((Client)clientList[i]).Online == true)
                {
                    sendData.ReadData.Clear();
                    sendData.ReadData.Add("ChatName", ((Client)clientList[i]).name);
                    sendData.ReadData.Add("ChatIp", ((Client)clientList[i]).IP);
                    sendData.ReadData.Add("ChatId", ((Client)clientList[i]).Id);
                    sendData.ReadData.Add("ChatDataIdentifier", DataIdentifier.UpdateList);
                    data = sendData.GetDataStream();
                    serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, clientEndPint, new AsyncCallback(this.SendData), clientEndPint);
                    Thread.Sleep(1);
                }

            }

        }

        #endregion
    }
}