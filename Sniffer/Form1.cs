using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace RawSocketCS
{
    public partial class Form1 : Form
    {
        public Socket rawSocket = null;

        public static int count = 0;
        public Form1()
        {
            InitializeComponent();
        }


        //const string HexValues = "0123456789ABCDEF";
        ////把字节数组转换为十六进制表示的字符串
        //private static string GetByteArrayHexString(byte[] buf, int startIndex, int size)
        //{
        //    StringBuilder sb = new StringBuilder(size * 5);
        //    sb.AppendFormat("{0,3:X}: ", 0);
        //    int j = 1;
        //    for (int i = startIndex, n = startIndex + size; i < n; i++, j++)
        //    {
        //        byte b = buf[i];
        //        char c = HexValues[(b & 0x0f0) >> 4];
        //        sb.Append(c);
        //        c = HexValues[(b & 0x0f)];
        //        sb.Append(c);
        //        sb.Append(' ');
        //        if ((j & 0x0f) == 0)
        //        {
        //            sb.Append(' ');
        //            //sb.Append(Encoding.ASCII.GetString(buf,i-15,8));
        //            AppendPrintableBytes(sb, buf, i - 15, 8);
        //            sb.Append(' ');
        //            //sb.Append(Encoding.ASCII.GetString(buf, i - 7, 8));
        //            AppendPrintableBytes(sb, buf, i - 7, 8);
        //            if (i + 1 != n)
        //            {
        //                sb.Append("\n");
        //                sb.AppendFormat("{0,3:X}: ", i - 1);    //偏移
        //            }
        //        }
        //        else if ((j & 0x07) == 0)
        //        {
        //            sb.Append(' ');
        //        }
        //    }
        //    int t;
        //    if ((t = ((j - 1) & 0x0f)) != 0)
        //    {
        //        for (int k = 0, kn = 16 - t; k < kn; k++)
        //        {
        //            sb.Append("   ");
        //        }
        //        if (t <= 8)
        //        {
        //            sb.Append(' ');
        //        }

        //        sb.Append(' ');
        //        //   sb.Append(Encoding.ASCII.GetString(buf, startIndex + size - t, t>8?8:t));
        //        AppendPrintableBytes(sb, buf, startIndex + size - t, t > 8 ? 8 : t);
        //        if (t > 8)
        //        {
        //            sb.Append(' ');
        //            //   sb.Append(Encoding.ASCII.GetString(buf, startIndex + size - t + 8, t - 8));
        //            AppendPrintableBytes(sb, buf, startIndex + size - t + 8, t - 8);
        //        }
        //    }
        //    return sb.ToString();
        //}

        //private static void AppendPrintableBytes(StringBuilder sb, byte[] buf, int startIndex, int len)
        //{
        //    for (int i = startIndex, n = startIndex + len; i < n; i++)
        //    {
        //        char c = (char)buf[i];
        //        if (!char.IsControl(c))
        //        {
        //            sb.Append(c);
        //        }
        //        else
        //        {
        //            sb.Append('.');
        //        }
        //    }
        //}


        /// <summary>
        /// 当点击的时候进行监听
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] inBytes = BitConverter.GetBytes(1);
                byte[] outBytes = BitConverter.GetBytes(0);
                //定义一个套接字
                rawSocket = new Socket(AddressFamily.InterNetwork,SocketType.Raw, ProtocolType.IP);

                IPHostEntry ips = Dns.GetHostEntry(Dns.GetHostName());
                if (ips != null)
                {
                    //rawSocket.Bind(new IPEndPoint(IPAddress.Parse("125.221.232.38"), 0));
                    
                    rawSocket.Bind(new IPEndPoint(ips.AddressList[3], 0));
                }
                else
                    return;
                

                rawSocket.IOControl(IOControlCode.ReceiveAll, inBytes, outBytes);
                this.button1.Enabled = false;
                Thread th = new Thread(CatchPacket);
                th.IsBackground = true;
                th.Start(rawSocket);

             
            }
            catch(SocketException e1) {
                Console.WriteLine(e1.Message);
            }
        }

        /// <summary>
        /// 抓包
        /// </summary>
        /// <param name="s"></param>
        private void CatchPacket(Object s)
        {
            Socket ss = s as Socket;//参数转换
            byte[] buffer = new byte[2400];//接收数据的缓冲区
            while (true)
            {
                if (count > 80000)
                    break;
                int j = ss.Receive(buffer);//接收数据
                if (j > 0)//若接收到数据包
                {
                    IPHeader ipheader = new IPHeader(buffer, j);
                    ListViewItem lv = new ListViewItem();//定义一个视图项
                    count += j;//count用于统计接收的总字节数

                    lv.Text = ipheader.srcAddr.ToString();//往视图项添加源IP
                    lv.SubItems.Add(ipheader.destAddr.ToString());//往视图项添加目的IP
                    if (ipheader.protocol == 6)//6代表TCP
                    {                                     
                            byte[] tcp = new byte[ipheader.length - ipheader.IPlength];
                            Array.Copy(buffer, ipheader.IPlength, tcp, 0, j - ipheader.IPlength);
                            TcpHeader tcpHeader = new TcpHeader(tcp, j);//解析TCP报文
                            lv.SubItems.Add(tcpHeader.sourcePort.ToString());
                            lv.SubItems.Add(tcpHeader.destinationPort.ToString());
                            lv.SubItems.Add("TCP");

                            ListViewItem tcpListView = new ListViewItem();
                            tcpListView.Text = tcpHeader.sourcePort.ToString();
                            tcpListView.SubItems.Add(tcpHeader.destinationPort.ToString());
                            tcpListView.SubItems.Add(tcpHeader.seq.ToString());
                            tcpListView.SubItems.Add(tcpHeader.ack.ToString());
                            tcpListView.SubItems.Add(tcpHeader.dataOffset.ToString());
                            tcpListView.SubItems.Add(tcpHeader.win.ToString());
                            this.listView2.Items.Add(tcpListView);
                       // if (tcpHeader.destinationPort == 80)
                       // {
                            string str = Encoding.UTF8.GetString(buffer,40,j-40);
                            this.richTextBox1.AppendText("\r\n" + str);
                      //  }
                    }
                    else if (ipheader.protocol == 17)
                    {//17代表UDP报文
                            byte[] udp = new byte[ipheader.length - ipheader.IPlength];
                            Array.Copy(buffer, ipheader.IPlength, udp, 0, j - ipheader.IPlength);
                            UdpHeader udpHeader = new UdpHeader(udp, j);//解析UDP报文
                            lv.SubItems.Add(udpHeader.sourcePort.ToString());
                            lv.SubItems.Add(udpHeader.destinationPort.ToString());
                            lv.SubItems.Add("UDP");
                    }
                    else
                    {//其他协议
                        lv.SubItems.Add(" ");
                        lv.SubItems.Add(" ");
                        lv.SubItems.Add("Others");
                    }
                    lv.SubItems.Add((ipheader.length).ToString());
                    lv.SubItems.Add(count.ToString());
                    this.listView1.Items.Add(lv);
                }
            }
        }

        public class UdpHeader
        {
            public UInt32 sourcePort;//源端口
            public UInt32 destinationPort;//目的端口
            public UInt32 udpLength;//Udp报文长度
            public UInt32 checkSum;//Udp报文校验码

            public UdpHeader(byte[] buf, int count)
            {
                sourcePort = (UInt32)(buf[0] << 8) + buf[1];
                destinationPort = (UInt32)(buf[2] << 8) + buf[3];
                udpLength = (UInt32)(buf[4] << 8) + buf[5];
                checkSum = (UInt32)(buf[6] << 8) + buf[7];
            }
        }


        //tcp头部
        public class TcpHeader
        {
            public UInt32 sourcePort;//16位源端口
            public UInt32 destinationPort;//16位目的端口
            public UInt32 seq;//32位序号
            public UInt32 ack;//32位确认号
            public byte dataOffset;//4位数据偏移
            public byte reserve;//6位保留
                                // public byte urg;//urg 1位
                                //public byte Ack;// ack 1位
                                // public byte psh;//psh 1位
                                // public byte rst;//rst 1位
                                // public byte syn;//1bit syn
                                //  public byte fin;//1bit fin
            public byte flag;//6位标志位；
            public UInt32 win;//16bit windows
            public UInt32 checkSum;//16bit check sum
            public UInt32 ptr;//urgent point

            public TcpHeader(byte[] buf,int len)
            {                
                sourcePort = ((UInt32)buf[0] << 8) + (UInt32)buf[1];//源端口
                destinationPort = ((UInt32)buf[2] << 8) + (UInt32)buf[3];//目的端口
                seq = ((UInt32)buf[7] << 24) + ((UInt32)buf[6] << 16) + ((UInt32)buf[5] << 8) + ((UInt32)buf[4]);
                ack = ((UInt32)buf[11] << 24) + ((UInt32)buf[10] << 16) + ((UInt32)buf[9] << 8) + ((UInt32)buf[8]);
                dataOffset = (byte)((buf[12] & 0xF0)>>2);
                reserve=(byte)(buf[12]& 0x0F + buf[13]& 0xC0);
                flag = (byte)(buf[13] & 0x3F);
                win = ((UInt32)buf[14] << 8) + buf[15];
                checkSum = ((UInt32)buf[17] << 8) + buf[16];
                ptr =((UInt32)buf[19] << 8) + buf[18];
            }
        }


        public class IPHeader
        {
            public int version;//4bit IP版本号
            public int IPlength;//4bit IP首部长度
            public byte tos; //8bit 区分服务
            public UInt32 length;//16bit 报文大小
            public int identify;//16bit 报文标识
            public int flag_and_frag;//16bit 报文标志和片位移
            public byte ttl;//8bit 生存时间
            public byte protocol;//8bit 报文协议
            public int chckSum;//16bit 校验和
            public IPAddress srcAddr; //源地址
            public IPAddress destAddr; //目标地址

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="buf">表示接收到的IP数据包</param>
            /// <param name="len">接收到的字节数</param>
            public IPHeader(byte[] buf, int len)
            {
                if (len > 20)//IP报文头部最小为20字节
                {
                    version = (buf[0] & 0xF0) >> 4;//buf[0]前四位表示版本号
                    IPlength = ((int)(buf[0] & 0x0F))*4;//后四位表示IP头长度
                    tos = buf[1];//头部服务
                    length = ((UInt32)buf[2] << 8 )+ (UInt32)buf[3];//IP报文长度
                    identify = ((int)buf[4] << 8) + (int)buf[5];//
                    flag_and_frag = ((int)buf[6] << 8) + (int)buf[7];
                    ttl = buf[8];
                    protocol = buf[9];
                    chckSum = (int)buf[10] + (int)buf[11];
                    byte[] addr = new byte[4];
                    for (int i = 0; i < 4; i++)
                        addr[i] = buf[12 + i];
                    srcAddr = new IPAddress(addr);
          
                    addr = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        addr[i] = buf[16 + i];
                    }
                    destAddr = new IPAddress(addr);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.listView1.Columns[0].Width = 130;
            this.listView1.Columns[1].Width = 130;
            this.listView1.Columns[2].Width = 90;
            this.listView1.Columns[3].Width = 90;
            this.listView1.Columns[4].Width = 90;
            this.listView1.Columns[5].Width = 90;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }
    }
}
