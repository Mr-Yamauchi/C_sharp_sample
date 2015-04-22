using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;



#pragma warning disable 618


namespace Net.Network
{
    public delegate void CallbackReceive(StateObject s);
    ///
    public class StateObject
    {
        public Socket Recv;
        public EndPoint EndP;
        public ManualResetEvent evt;
        public byte[] rdata;
    }
    ///	
    public class StartObject
    {
        public StateObject state;
        public CallbackReceive fp;
    }
    ///
    public class StateObjectSend
    {
        public Socket workSocket;
    }
    ///
    public class NetSend
    {
        public int UseInterface { get; set; }
        public int Port { get; set; }
        public string UcastAddress { get; set; }
        public string McastAddress { get; set; }
        private Socket UcastSocket;
        private Socket McastSocketSend;
        private Socket McastSocketRecv;
        private IPEndPoint EpUcast;
        private IPEndPoint EpMcast;
        private IPEndPoint EpUcastRecv;
        private IPEndPoint EpMcastRecv;

        public NetSend(int ifno, int ports, string uaddr, string maddr)
        {
            this.UseInterface = ifno;
            this.Port = ports;
            this.UcastAddress = uaddr;
            this.McastAddress = maddr;
        }

        public int Initialize()
        {
	        string localName = Dns.GetHostName();
           
	        IPAddress localAddress = Dns.GetHostAddresses(localName)[this.UseInterface];

            IPAddress[] ladr = Dns.GetHostAddresses(localName);

            IPAddress mcastAddress = IPAddress.Parse(this.McastAddress);
            IPAddress targetAddress = IPAddress.Parse(this.UcastAddress);

            this.EpUcast = new IPEndPoint(targetAddress, this.Port);
            this.EpMcast = new IPEndPoint(mcastAddress, this.Port - 1);
            this.EpUcastRecv = new IPEndPoint(localAddress, this.Port);
            this.EpMcastRecv = new IPEndPoint(mcastAddress, this.Port - 1);

            UcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            McastSocketSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            McastSocketRecv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            UcastSocket.Bind(new IPEndPoint(localAddress, this.Port));
            McastSocketSend.Bind(new IPEndPoint(localAddress, this.Port - 1));
            McastSocketRecv.Bind(new IPEndPoint(IPAddress.Any, this.Port - 1));

            UcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            MulticastOption mcastOpt = new MulticastOption(mcastAddress, localAddress);

            McastSocketSend.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            McastSocketRecv.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            McastSocketRecv.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)localAddress.Address);
            McastSocketSend.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            McastSocketRecv.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            McastSocketSend.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOpt);
            McastSocketRecv.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOpt);


            return (0);
        }
        private int SendData(Socket so, byte[] rdata, IPEndPoint p)
        {
            StateObjectSend state = new StateObjectSend();
            state.workSocket = so;

            AsyncCallback callback = (ar) =>
            {
                StateObjectSend si = (StateObjectSend)(ar.AsyncState);
                Socket s = si.workSocket;

                int send = s.EndSendTo(ar);
                System.Console.WriteLine("send {0}", send);
            };

            IAsyncResult result = so.BeginSendTo(rdata, 0, rdata.Length, 0, p, callback, state);
            //result.AsyncWaitHandle.WaitOne();

            return (0);
        }
        public int McastSend(byte[] rdata)
        {
            SendData(this.McastSocketSend, rdata, this.EpMcast);
            return (0);
        }
        public int UcastSend(byte[] rdata)
        {
            SendData(this.UcastSocket, rdata, this.EpUcast);
            return (0);
        }
        private void RecvThread(object s)
        {
            StartObject ss = (StartObject)s;
            StateObject st = (StateObject)ss.state;
            CallbackReceive call = (CallbackReceive)ss.fp;
            while (true)
            {
                System.Console.WriteLine("Wait packer ......");
                byte[] b = new byte[1500];
                st.Recv.ReceiveFrom(b, ref st.EndP);
                st.rdata = b;
                if (st != null)
                {
                    call(st);
                }
            }
        }

        public void RecvStart(CallbackReceive mcastCb, CallbackReceive ucastCb)
        {
            StartObject ss = new StartObject();
            StateObject so = new StateObject();
            so.Recv = this.McastSocketRecv;
            so.EndP = this.EpMcastRecv;
            ss.state = so;
            //ss.fp = new Net.Network.CallbackReceive(NetQueue.CallbackRecv);
            ss.fp = mcastCb;
            Thread th = new Thread(new ParameterizedThreadStart(this.RecvThread));
            th.Start(ss);

            StartObject ss2 = new StartObject();
            StateObject so2= new StateObject();
            so2.Recv = this.UcastSocket;
            so2.EndP = this.EpUcastRecv;
            ss2.state = so2;
            //ss2.fp = new Net.Network.CallbackReceive(NetQueue.CallbackRecv);
            ss2.fp = ucastCb;
            Thread th2 = new Thread(new ParameterizedThreadStart(this.RecvThread));
            th2.Start(ss2);
        }
    }
    ///
    public class SendObject 
    {
        public byte[] data;
    }
    ///
    public class NetQueue
    {

	    private Queue<StateObject> RecvQueue;	
	    private Queue<StateObject> SendQueue;	
	    //
	    public NetQueue(){
		    RecvQueue = new Queue<StateObject>();
		    SendQueue = new Queue<StateObject>();
	    }
	    public StateObject GetRecvQueue(){
	        return RecvQueue.Dequeue();	
	    }
	    public StateObject GetSendQueue(){
	        return SendQueue.Dequeue();	
	    }
	    private int SetQueue(Queue<StateObject> q, byte[] d)
	    {
		    StateObject s = new StateObject();
		    s.rdata = d;
		    q.Enqueue(s);
            System.Console.WriteLine("{0} count {1}", q, q.Count);
		    return 0;
	    }
	    public int SetSendQueue(byte[] d){
		    SetQueue(this.SendQueue, d);
		    return 0;
    	}
	    private int SetRecvQueue(byte[] d){
		    SetQueue(this.RecvQueue, d);
		    return 0;
	    }
	    //
        public void CallbackRecv(StateObject s)
        {
	        SetRecvQueue(s.rdata);
            //string str = System.Text.Encoding.ASCII.GetString(s.rdata,0,s.rdata.Length);
            var mb = new Message.MessageBase();
            var hb = mb.GetMessageData<Message.MessageHeaderBase>(s.rdata);

            switch (hb.type)
            {
                case Message.MessageType.MESSAGE_TYPE_MEMB_JOIN:
                    var mc = new Message.MessageJoin();
                    var req = mc.GetMessageData<Message.MessageJoinReq>(s.rdata);
                    mc.DumpMessageJoinReq(req);
                    break;
            }
        }
    }

}
