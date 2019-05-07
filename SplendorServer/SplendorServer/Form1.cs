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

using GameClassDefine;
using PacketDefine;


namespace SplendorServer
{
    public partial class Form1 : Form
    {
        // Server
        public const int PORT = 7777;
        public string IP;

        public bool m_bStop = false;
        private TcpListener m_server;
        private Thread m_thServer;

        // Server Packet
        public NetworkStream m_stream1;
        public NetworkStream m_stream2;
        private byte[] sendBuffer = new byte[1024 * 4];
        private byte[] readBuffer = new byte[1024 * 4];

        /*
        public Initialize m_initializeClass;
        public BeforeSelect m_beforeSelectClass;
        public BeforeExpand m_beforeExpandClass;
        public FileTransfer m_fileTransferClass;
        public ExitConnection m_exitConnectionClass;
        */

        // Client and Connect
        public bool m_bConnect1 = false;
        public bool m_bConnect2 = false;
        TcpClient m_client1;
        TcpClient m_client2;

        private int clientCnt = 0;

        // Board Info
        Player player1 = new Player();
        Player player2 = new Player();
        Board board = new Board();


        public Form1()
        {
            InitializeComponent();
        }

        public void GetLocalIP()
        {
            string localIP = "Not available, please check your network seetings!";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            IP =  localIP;
        }

        public void WriteLog(string msg)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                txtLog.AppendText(msg + "\n");
            }));
        }

        public void Send1(int num)
        {
            if(num == 1)
            {
                m_stream1.Write(sendBuffer, 0, sendBuffer.Length);
                m_stream1.Flush();
            }
            else if(num == 2)
            {
                m_stream2.Write(sendBuffer, 0, sendBuffer.Length);
                m_stream2.Flush();
            }

            for (int i = 0; i < 1024 * 4; i++)
            {
                sendBuffer[i] = 0;
            }
        }

        public void GameInit()
        {
           
        }

        public void ServerStart()
        {
            try
            {
                IPAddress ipAddr = IPAddress.Parse(IP);

                m_server = new TcpListener(ipAddr, PORT);
                m_server.Start();

                m_bStop = true;
                WriteLog("플레이어 접속 대기중...");

                while (m_bStop)
                {
                    m_client1 = m_server.AcceptTcpClient();
                    if (m_client1.Connected)
                    {
                        m_bConnect1 = true;
                        clientCnt++;
                        WriteLog("플레이어1 접속");
                        m_stream1 = m_client1.GetStream();
                    }

                    m_client2 = m_server.AcceptTcpClient();
                    if (m_client2.Connected)
                    {
                        m_bConnect2 = true;
                        clientCnt++;
                        WriteLog("플레이어2 접속");
                        m_stream2 = m_client2.GetStream();
                    }

                    GameInit();

                    while (m_bConnect1 && m_bConnect2)
                    {
                    
                        // 플레이어1 턴

                        // 플레이어2 턴

                        /*
                        try
                        {
                            m_stream.Read(readBuffer, 0, 1024 * 4);
                        }
                        catch
                        {
                            WriteLog("서버에서 데이터를 읽는데 에러가 발생해 서버를 종료합니다.");
                            ServerStop();
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                btnServer.Text = "서버켜기";
                                btnServer.ForeColor = Color.Black;
                            }));
                            return;
                        }

                        Packet packet = (Packet)Packet.Desserialize(readBuffer);
                        */
                        /*
                        switch ((int)packet.Type)
                        {
                            case (int)PacketType.init:
                                {
                                    m_initializeClass = (Initialize)Packet.Desserialize(readBuffer);
                                    WriteLog("초기화 데이터 요청..");

                                    
                                    byte[] bytePath = Encoding.UTF8.GetBytes(dirPath);
                                    m_stream.Write(bytePath, 0, bytePath.Length);
                                    break;
                                }
                            case (int)PacketType.beforeSelect:
                                {
                                    m_beforeSelectClass = (BeforeSelect)Packet.Desserialize(readBuffer);
                                    WriteLog("beforeSelect 데이터 요청..");
                                    string path = m_beforeSelectClass.path;

                                    
                                    DirectoryInfo di;
                                    BeforeSelect bs = new BeforeSelect();
                                    try
                                    {
                                        di = new DirectoryInfo(path);
                                        bs.Type = (int)PacketType.beforeSelect;
                                        bs.diArray = di.GetDirectories();
                                        bs.fiArray = di.GetFiles();
                                        Packet.Serialize(bs).CopyTo(sendBuffer, 0);
                                        Send();
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteLog("BeforeSelect error " + ex.Message);
                                    }
                                    break;
                                }
                            case (int)PacketType.beforeExpand:
                                {
                                    m_beforeExpandClass = (BeforeExpand)Packet.Desserialize(readBuffer);
                                    WriteLog("beforeExpand 데이터 요청..");
                                    string path = m_beforeExpandClass.path;

                                    
                                    DirectoryInfo di;
                                    DirectoryInfo diPlus;
                                    DirectoryInfo[] diArrayPlus;
                                    BeforeExpand be = new BeforeExpand();
                                    try
                                    {
                                        di = new DirectoryInfo(path);
                                        be.Type = (int)PacketType.beforeExpand;
                                        be.diArray = di.GetDirectories();

                                        
                                        be.diAdd = new Dictionary<string, int>();
                                        foreach (DirectoryInfo dir in be.diArray)
                                        {
                                            diPlus = new DirectoryInfo(dir.FullName);
                                            diArrayPlus = diPlus.GetDirectories();
                                            if (diArrayPlus.Length > 0)
                                                be.diAdd.Add(dir.Name, 1);
                                            else
                                                be.diAdd.Add(dir.Name, 0);
                                        }
                                        Packet.Serialize(be).CopyTo(sendBuffer, 0);
                                        Send();
                                    }
                                    catch (Exception ex)
                                    {
                                        WriteLog("BeforeExpand error " + ex.Message);
                                    }
                                    break;
                                }
                            case (int)PacketType.fileTransfer:
                                {
                                    m_fileTransferClass = (FileTransfer)Packet.Desserialize(readBuffer);
                                    WriteLog("파일 전송 요청..");
                                    string path = m_fileTransferClass.path;
                                    long size = m_fileTransferClass.size;
                                    byte[] fileSendBuffer = new byte[1024];

                                    
                                    FileStream fStr = new FileStream(path, FileMode.Open, FileAccess.Read);
                                    BinaryReader bReader = new BinaryReader(fStr);
                                    long loopCnt = (long)(size / 1024 + 1);

                                    try
                                    {
                                       
                                        int reSize = 1024;
                                        for (long i = 0; i < loopCnt; i++)
                                        {
                                            if (i == loopCnt - 1)
                                                reSize = (int)(size - (1024 * (loopCnt - 1)));

                                            fileSendBuffer = bReader.ReadBytes(reSize);
                                            m_stream.Write(fileSendBuffer, 0, reSize);
                                            m_stream.Flush();

                                            
                                            for (int j = 0; j < reSize; j++)
                                            {
                                                fileSendBuffer[j] = 0;
                                            }
                                        }

                                        WriteLog(path + " 파일 전송 완료");

                                    }
                                    catch (Exception ex)
                                    {
                                        WriteLog("FileTransfer error " + ex.Message);
                                    }
                                    finally
                                    {
                                        fStr.Close();
                                        bReader.Close();
                                    }
                                    break;
                                }
                            case (int)PacketType.exitConnection:
                                {
                                    WriteLog("클라이언트 연결 해제");

                                    
                                    m_bConnect = false;
                                    m_stream.Close();
                                    break;
                                }
                        }
                        */
                    }
                }
            }

            catch (Exception ex)
            {
                WriteLog(ex.Message + " 예외 발생으로 인해 서버를 종료합니다.");
                ServerStop();
                this.Invoke(new MethodInvoker(delegate ()
                {
                    btnServer.Text = "서버켜기";
                    btnServer.ForeColor = Color.Black;
                }));
                return;
            }
        }

        public void ServerStop()
        {
            if (!m_bStop)
                return;

            m_bStop = false;
            m_bConnect1 = false;
            m_bConnect2 = false;
            m_server.Stop();
            m_stream1.Close();
            m_stream2.Close();
            m_thServer.Abort();
            WriteLog("서버 종료");
        }

        private void btnServer_Click(object sender, EventArgs e)
        {
            if (btnServer.Text == "서버켜기")
            {
                m_thServer = new Thread(new ThreadStart(ServerStart));
                m_thServer.Start();

                btnServer.Text = "서버끊기";
                btnServer.ForeColor = Color.Red;
            }
            else
            {
                ServerStop();
                btnServer.Text = "서버켜기";
                btnServer.ForeColor = Color.Black;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetLocalIP();
            txtIP.Text = IP;
        }
    }
}
