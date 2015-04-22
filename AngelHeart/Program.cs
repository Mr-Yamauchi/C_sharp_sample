using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Net.Network;
using Net.Message;


namespace AngelHeart
{
    class Program
    {
        static void Main(string[] args)
        {
            var sender = new NetSend(1, 5405, "172.30.82.40", "226.94.1.1");
            sender.Initialize();
		
            var mcastqueue = new NetQueue();
            var ucastqueue = new NetQueue();

            sender.RecvStart(mcastqueue.CallbackRecv, ucastqueue.CallbackRecv);

/*
    string str = "test";
    byte[] byteArray = Encoding.Unicode.GetBytes(str);
    
    queue.SetSendQueue(byteArray);
    StateObject ss = queue.GetSendQueue();
    string s = Encoding.Unicode.GetString(ss.rdata);
    System.Console.WriteLine("test string --> {0}", s);
*/
            var mc = new Net.Message.MessageJoin();
            var mj = new Net.Message.MessageJoinReq();
            mj.type = Net.Message.MessageType.MESSAGE_TYPE_MEMB_JOIN;
            mj.mid = 10;

            
 /*           if (args.Length != 0)
            {*/
            string data = "This is a multicast packet";
            for (int i = 0; i < 5; i++)
            {
                mj.mid = 20;
                mj.mstr = data;
                sender.McastSend(mc.GetMessageBytes(mj));
            }
                    // Send unicast packets.
                data = "This is a unicast packet---------------";
                mj.mstr = data;
                sender.UcastSend(mc.GetMessageBytes(mj));

                // Send bcast packets.
/*
                data = "Thit is a bcast packe !!!";
                mj.mstr = data;
                mj.mid = 30;
                sender.BcastSend(mc.GetMessageBytes(mj));
 */
/*            }*/
          
            System.Console.ReadLine();

        }
    }
}
