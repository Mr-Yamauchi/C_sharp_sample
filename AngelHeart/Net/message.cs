using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable 414
#pragma warning disable 169

namespace Net.Message
{
    //
    enum MEMB_STATE
    {
        MEMB_STATE_OPERATION = 1,
        MEMB_STATE_GATHER = 2,
        MEMB_STATE_COMMIT = 3,
        MEMB_STATE_RECOVERY = 4
    };
    //
    static class MessageType
    {
        public const uint MESSAGE_TYPT_ORF_TOKEN = 1;
        public const uint MESSAGE_TYPE_MCAST = 2;
        public const uint MESSAGE_TYPE_MEMB_MERGE_DETECT = 3;
        public const uint MESSAGE_TYPE_MEMB_JOIN = 4;
        public const uint MESSAGE_TYPE_MEMB_COMMT_TOKEN = 5;
        public const uint MESSAGE_TYPE_TOKEN_HOLD_CANCEL = 6;
    }
    //
    enum ENCAPSULATION_TYPE
    {
        MESSAGE_ENCAPSULATED = 1,
        MESSAGE_NOT_ENCAPSULATED = 2
    };
    //
    [Serializable()]
    public class TotemIpAddress
    {
        uint nodeid;
        ushort family;
        byte[] addr = new byte[128];
    }
    //
    [Serializable()]
    public class MemberRingId
    {
        TotemIpAddress rep = new TotemIpAddress();
        ulong seq;
    }
    //
    [Serializable()]
    public class SrpAddr
    {
        public TotemIpAddress[] totem_ip_address = new TotemIpAddress[2];
    }
    //
    [Serializable()]
    public abstract class MessageHeaderBase
    {
        public uint type { get; set; }
        char encapsulated;
        ushort endian_detector;
        uint nodeid;
    }
    //
    [Serializable()]
    public class MessageJoinReq : MessageHeaderBase
    {
        SrpAddr system_from = new SrpAddr();
        List<SrpAddr> proc_list = new List<SrpAddr>();
        List<SrpAddr> failed_list = new List<SrpAddr>();
        ulong ring_seq;
        public int mid;
        public string mstr;
    }
    //
    [Serializable()]
    public class MessageMergeDetect : MessageHeaderBase
    {
        SrpAddr system_from = new SrpAddr();
        MemberRingId ring_id = new MemberRingId();
    }
    //
    [Serializable()]
    public class TokenHoldCancel : MessageHeaderBase
    {
        MemberRingId ring_id = new MemberRingId();
    }
    //
    [Serializable()]
    public class MembCommitTokenMembEntry
    {
        MemberRingId ringid = new MemberRingId();
        uint aru;
        uint high_delivered;
        uint receive_flg;
    }
    //
    [Serializable()]
    public class MembCommitToken : MessageHeaderBase
    {
        uint token_seq;
        MemberRingId ring_id = new MemberRingId();
        uint retrans_flg;
        int memb_index;
        List<MembCommitTokenMembEntry> memb_list = new List<MembCommitTokenMembEntry>();
    }
    //
    public class MessageBase
    {
        //
        public byte[] GetMessageBytes(object obj)
        {
            var formatter = new BinaryFormatter();
            var mem = new MemoryStream();
            formatter.Serialize(mem, obj);
            byte[] byteData = mem.ToArray();
            mem.Close();

            return byteData;
        }
        //
        public T GetMessageData<T>(byte[] b)
        {
            T ret;

            var mem = new MemoryStream(b.Length);
            mem.Write(b, 0, b.Length);
            mem.Seek(0, SeekOrigin.Begin);
            var formmater = new BinaryFormatter();
            ret = (T)formmater.Deserialize(mem);
            mem.Close();

            return ret;
        }
        //
        public void DumpMessage<T1>(Action<T1> d, T1 value)
        {
            d(value);
        }
    }
    //
    public class MessageJoin : MessageBase
    {
        //
        public void DumpMessageJoinReq(MessageJoinReq m)
        {
            DumpMessage<MessageJoinReq>((r) => { System.Console.WriteLine("message JOIN ---- {0} --- {1}", r.mid, r.mstr); }, m);
        }
    }

}
