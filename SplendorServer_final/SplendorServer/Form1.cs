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
        private byte[] sendBuffer = new byte[1024 * 20];
        private byte[] readBuffer = new byte[1024 * 20];

        //public Init m_InitClass;
        public Gem m_GemClass;
        public SelectCard m_SelectCard;
        public TurnEnd m_TurnEnd;

        // Client and Connect
        public bool m_bConnect1 = false;
        public bool m_bConnect2 = false;
        TcpClient m_client1;
        TcpClient m_client2;

        // Board Info
        Player[] gamePlayers = new Player[2];       // 현재 플레이어 상태(0 - 플레이어1, 1- 플레이어2)
        Board board = new Board();                  // 현재 보드 상태
        ActiveCard activeCard;                      // 카드 활성화 상태
        public int turn = 1;                        // 현재 턴을 저장

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
                txtLog.AppendText(msg + "\r\n");
            }));
        }

        public void Send(int num)
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

            for (int i = 0; i < 1024 * 20; i++)
            {
                sendBuffer[i] = 0;
            }
        }

        public void ReadStream(int num)
        {
            if(num == 1)
                m_stream1.Read(readBuffer, 0, 1024 * 20);
            else if(num == 2)
                m_stream2.Read(readBuffer, 0, 1024 * 20);
        }

        public void SendAndTrunEnd(TurnEnd te, int turn)
        {
            if (turn == 1)
            {
                // Player 1

                Packet.Serialize(te).CopyTo(sendBuffer, 0);
                Send(1);

                // 상대방 플레이어 카드 활성화 전송
                te.activeCard = activeCard;
                Packet.Serialize(te).CopyTo(sendBuffer, 0);
                Send(2);

                turn = 2;
            }
            else if (turn == 2)
            {
                // Player 2

                Packet.Serialize(te).CopyTo(sendBuffer, 0);
                Send(2);

                // 상대방 플레이어 카드 활성화 전송
                te.activeCard = activeCard;
                Packet.Serialize(te).CopyTo(sendBuffer, 0);
                Send(1);

                turn = 1;
            }
        }

        public void GameInit()
        {
            /*
             * 1. 클라이언트에게 보드 정보, 플레이어 초기 정보 (Turn End 타입) 전송
             *    (Player1 Send) (Player2 Send)
             */
            gamePlayers[0] = new Player();
            gamePlayers[1] = new Player();

            TurnEnd msg = new TurnEnd();
            msg.Type = (int)PacketType.turnEnd;

            msg.chosenGems = null;
            msg.chosenCardID = -1;
            msg.chosenDeck = -1;
            msg.chosenNobleID = -1;
            msg.players = gamePlayers;
            msg.boardInfo = board;
            msg.activeCard = null;
            msg.winner = 0;
            msg.turnPlayer = 1;     // 플레이어1 먼저 수행하도록

            WriteLog("Player1, Player2 - 초기 보드 정보, 플레이어 정보 전송");

            Packet.Serialize(msg).CopyTo(sendBuffer, 0);
            Send(1);
            Packet.Serialize(msg).CopyTo(sendBuffer, 0);
            Send(2);
        }

        public bool GemIsValid()
        {
            int[] selectedGems = m_GemClass.gems;
            bool returnValue = true;
            int gemCnt = 0;
            int twoGemCnt = 0;

            for(int i = 0; i < 5; i++)
            {
                if (selectedGems[i] == 1)
                    gemCnt++;

                // 하나의 보석을 두 개 가져온 경우
                if (selectedGems[i] == 2)
                {
                    if(board.boardGems[i] < 4)
                    {
                        // 위배 - 보석이 4개 이하인 경우
                        returnValue = false;
                        break;
                    }
                    else
                    {
                        twoGemCnt++;
                    }
                }

                // 위배 - 하나의 보석을 세 개 이상 가져온 경우
                if (selectedGems[i] > 2)
                    returnValue = false;
                
            }

            if (gemCnt != 3 || twoGemCnt != 1)
                returnValue = false;

            return returnValue;
        }

        void CardActivate()
        {
            activeCard = new ActiveCard();
            int num = 0;

            if (turn == 1)
                num = 1;
            else if (turn == 2)
                num = 0;

            int[] currentPlayerGem = new int[5];

            // 현재 플레이어의 보석과 할인 받을 수 있는 보석을 계산해 가져오기
            for(int i = 0; i < 5; i++)
            {
                currentPlayerGem[i] = gamePlayers[num].gemSale[i] + gamePlayers[num].playerGems[i];
            }

            // i : 카드 변수, j : 잼 변수
            for (int i = 0; i < 4; i++)
            {
                bool isActiveOne = true;
                bool isActiveTwo = true;
                bool isActiveThree = true;

                for (int j = 0; j < 5; j++)
                {
                    // 레벨1 카드 검사
                    if (board.boardCards1[i].cardCost[j] > currentPlayerGem[j])
                    {
                        isActiveOne = false;
                        break;
                    }
                }
                // 레벨1 카드 활성화
                if (isActiveOne)
                    activeCard.activeCards1[i] = true;

                for (int j = 0; j < 5; j++)
                {
                    // 레벨2 카드 검사
                    if (board.boardCards2[i].cardCost[j] > currentPlayerGem[j])
                    {
                        isActiveTwo = false;
                        break;
                    }
                }
                // 레벨2 카드 활성화
                if (isActiveTwo)
                    activeCard.activeCards2[i] = true;

                for (int j = 0; j < 5; j++)
                {
                    // 레벨3 카드 검사
                    if (board.boardCards3[i].cardCost[j] > currentPlayerGem[j])
                    {
                        isActiveThree = false;
                        break;
                    }
                }
                // 레벨3 카드 활성화
                if (isActiveThree)
                    activeCard.activeCards3[i] = true;
            }
        }

        int CheckNoble(int playerNum)
        {
            foreach (var chkNoble in board.boardNoble)
            {
                // 보드에 있는 귀족 검사

                int i = 0;
                for (i = 0; i < 5; i++)
                {
                    if (chkNoble.nobleCost[i] > gamePlayers[playerNum].gemSale[i]) // 각 보석마다 값 비교
                        break; // 귀족 보석 cost이 더 크면 반복문 벗어남
                }
                if (i == 5)
                {
                    // 구매 가능한 경우

                    // 플레이어와 보드의 귀족카드를 업데이트
                    gamePlayers[playerNum].playerNoble.Add(chkNoble);
                    gamePlayers[playerNum].totalScore += 3;
                    board.boardNoble.Remove(chkNoble);
                    board.DrawCard(4);
                    return chkNoble.nobleID;
                }
            }
            return -1;
        }


        int purchaseCard(SelectCard mSelectCard, int playerNum)
        {
            int id = mSelectCard.cardId;
            int level = 0;

            if (id < 41)
            {
                level = 1;
            }
            else if (id < 71)
            {
                level = 2;
            }
            else if (id < 91)
            {
                level = 3;
            }

            // 보드에서 구매한 카드 제거, 덱에서 보드에 새로운 카드 추가
            if (level == 1) // level1
            {
                int i = 0;
                while (id != board.boardCards1[i].cardID)
                {
                    i++;
                }

                // 플레이어 보유 보석에서 카드 비용 제거
                for (int n = 0; n < 5; n++)
                {
                    gamePlayers[playerNum].playerGems[n] =
                        gamePlayers[playerNum].playerGems[n] -
                        (board.boardCards1[n].cardCost[n] - gamePlayers[playerNum].gemSale[playerNum]);
                }
                // 플레이어1 보유 카드 목록에 추가
                gamePlayers[playerNum].playerCards.Add(board.boardCards1[i]);
                // 플레이어 점수 증가
                gamePlayers[playerNum].totalScore += board.boardCards1[i].cardScore;

                board.boardCards1.Remove(board.boardCards1[i]);
                board.DrawCard(1);
            }
            else if (level == 2) // level2
            {
                int i = 0;
                while (id != board.boardCards2[i].cardID)
                {
                    i++;
                }

                // 플레이어 보유 보석에서 카드 비용 제거
                for (int n = 0; n < 5; n++)
                {
                    gamePlayers[playerNum].playerGems[n] =
                        gamePlayers[playerNum].playerGems[n] -
                        (board.boardCards2[n].cardCost[n] - gamePlayers[playerNum].gemSale[playerNum]);
                }
                // 플레이어1 보유 카드 목록에 추가
                gamePlayers[playerNum].playerCards.Add(board.boardCards2[i]);
                // 플레이어 점수 증가
                gamePlayers[playerNum].totalScore += board.boardCards2[i].cardScore;

                board.boardCards2.Remove(board.boardCards2[i]);
                board.DrawCard(2);
            }
            else if (level == 3) // level3
            {
                int i = 0;
                while (id != board.boardCards3[i].cardID)
                {
                    i++;
                }

                // 플레이어 보유 보석에서 카드 비용 제거
                for (int n = 0; n < 5; n++)
                {
                    gamePlayers[playerNum].playerGems[n] =
                        gamePlayers[playerNum].playerGems[n] -
                        (board.boardCards3[n].cardCost[n] - gamePlayers[playerNum].gemSale[playerNum]);
                }
                // 플레이어1 보유 카드 목록에 추가
                gamePlayers[playerNum].playerCards.Add(board.boardCards3[i]);
                // 플레이어 점수 증가
                gamePlayers[playerNum].totalScore += board.boardCards3[i].cardScore;

                board.boardCards3.Remove(board.boardCards3[i]);
                board.DrawCard(3);
            }

            return id;
        }

        int checkWinner()
        {
            // 1: 플레이어1 승리
            // 2: 플레이어2 승리
            // -1: 무승부
            // 0: 승리자 없음 게임 진행

            int scoreOne = gamePlayers[0].totalScore;
            int scoreTwo = gamePlayers[1].totalScore;
            int cardNumOne = gamePlayers[0].playerCards.Count;
            int cardNumTwo = gamePlayers[1].playerCards.Count;

            if (scoreOne >= 15 || scoreTwo >= 15)
            {
                if (scoreOne > scoreTwo)
                    return 1;
                else if (scoreOne < scoreTwo)
                    return 2;
                else // 동점 상황
                {
                    if (cardNumOne > cardNumTwo)
                        return 2;
                    else if (cardNumOne < cardNumTwo)
                        return 1;
                    else // 카드 개수까지 동일
                        return -1;
                }
            }
            else // 승리자 없음
                return 0;
        }

        public void ServerStart()
        {
            

            try
            {
                IPAddress ipAddr = IPAddress.Parse(IP);

                m_server = new TcpListener(ipAddr, PORT);
                m_server.Start();

                m_bStop = true;
                WriteLog("Player 접속 대기중...");

                while (m_bStop)
                {
                    Init init = new Init();

                    // 플레이어 1 연결
                    m_client1 = m_server.AcceptTcpClient();
                    if (m_client1.Connected)
                    {
                        m_bConnect1 = true;
                        WriteLog("Player1 접속");
                        m_stream1 = m_client1.GetStream();

                        init.playerNum = 1;
                        Packet.Serialize(init).CopyTo(sendBuffer, 0);
                        Send(1);
                    }
                    init.playerNum = 0;

                    // 플레이어 2 연결
                    m_client2 = m_server.AcceptTcpClient();
                    if (m_client2.Connected)
                    {
                        m_bConnect2 = true;
                        WriteLog("Player2 접속");
                        m_stream2 = m_client2.GetStream();

                        init.playerNum = 2;
                        Packet.Serialize(init).CopyTo(sendBuffer, 0);
                        Send(2);
                    }

                    // 클라이언트에게 보드 정보, 각 플레이어 초기 정보 전송
                    GameInit();

                    // 게임 시작
                    while (m_bConnect1 && m_bConnect2)
                    {
                        // Player 1 == turn 1
                        // Player 2 == turn 2
                        while (turn == 1 || turn == 2)
                        {
                            try
                            {   
                                // 1 : Player1 stream
                                // 2 : Player2 stream
                                ReadStream(turn);
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

                            switch ((int)packet.Type)
                            {
                                case (int)PacketType.gem:
                                    {
                                        /*
                                         * 1. 플레이어가 선택한 보석이 유효한지 검사                    - GemIsVaild()
                                         * 2. 1을 위배한다면 상태 값을 변경해 클라이언트에게 전송       - (Player1 Send)
                                         *     
                                         * 3. 서버측에서 플레이어 보유하고 있는 보석 정보를 업데이트 & 현재 보드 업데이트
                                         * 4. 상대방 플레이어 카드 활성화 계산 수행                     - CardActivate(1)
                                         * 5. 귀족 방문 여부 체크                                       - CheckNoble(0)
                                         * 6. 승리 조건 검사                                            - CheckWinner(0)
                                         * 7. 플레이어1,2 에게 정보 전송
                                         */

                                        m_GemClass = (Gem)Packet.Desserialize(readBuffer);
                                        WriteLog("Player" + turn  + " - 보석 선택");

                                        if (!GemIsValid())
                                        {
                                            // 보석이 유효하지 않는 경우
                                            WriteLog("Player" + turn + " - 보석이 유효하지 않습니다");
                                            Gem sendInValid = new Gem();
                                            sendInValid.Type = (int)PacketType.gem;

                                            sendInValid.gemStatus = true;
                                            Packet.Serialize(sendInValid).CopyTo(sendBuffer, 0);
                                            Send(turn);
                                            break;
                                        }
                                        else
                                        {
                                            // 보석이 유효한 경우
                                            Gem sendValid = new Gem();
                                            sendValid.Type = (int)PacketType.gem;

                                            sendValid.gemStatus = false;
                                            Packet.Serialize(sendValid).CopyTo(sendBuffer, 0);
                                            Send(turn);
                                        }

                                        // 플레이어가 보유하고 있는 보석 정보를 업데이트 & 현재 보드 정보 업데이트
                                        for (int i = 0; i < 5; i++)
                                        {
                                            gamePlayers[turn - 1].playerGems[i] += m_GemClass.gems[i];
                                            board.boardGems[i] -= m_GemClass.gems[i];
                                        }

                                        CardActivate();

                                        // 귀족 방문 여부 체크
                                        int nobleID = CheckNoble(turn - 1);

                                        // 승리 조건 검사
                                        int winnerNum = checkWinner();

                                        // 플레이어1,2에게 TurnEnd 패킷 전송
                                        TurnEnd sendStatus = new TurnEnd();
                                        sendStatus.Type = (int)PacketType.turnEnd;

                                        sendStatus.chosenGems = m_GemClass.gems;
                                        sendStatus.chosenCardID = -1;
                                        sendStatus.chosenDeck = -1;
                                        sendStatus.chosenNobleID = nobleID;
                                        sendStatus.players = gamePlayers;
                                        sendStatus.boardInfo = board;
                                        sendStatus.activeCard = null;
                                        sendStatus.winner = winnerNum;
                                        sendStatus.turnPlayer = 2;

                                        WriteLog("Player" + turn + " - 턴 종료");

                                        // 데이터 전송 및 턴 변경
                                        SendAndTrunEnd(sendStatus, turn);
                                        
                                        break;
                                    }
                                case (int)PacketType.card:
                                    {
                                        this.m_SelectCard = (SelectCard)Packet.Desserialize(this.readBuffer);

                                        // 로그 출력
                                        WriteLog("Player" + turn + " - 카드 선택");

                                        // 상대방 카드 활성화 여부 함수 호출
                                        CardActivate();

                                        TurnEnd te = new TurnEnd();

                                        // 카드 구매
                                        int cardID = purchaseCard(m_SelectCard, turn - 1);

                                        // 귀족 방문 여부 검사
                                        int nobleID = CheckNoble(turn - 1);

                                        // 승리 여부 검사
                                        int winnerNum = checkWinner();

                                        // 선택 카드 레벨 추출
                                        int id = m_SelectCard.cardId;
                                        int level = 0;

                                        if (id < 41)
                                        {
                                            level = 1;
                                        }
                                        else if (id < 71)
                                        {
                                            level = 2;
                                        }
                                        else if (id < 91)
                                        {
                                            level = 3;
                                        }

                                        // TurnEnd 클래스 정보 수정
                                        te.chosenGems = null;
                                        te.chosenCardID = cardID;
                                        te.chosenDeck = level;
                                        te.chosenNobleID = nobleID;
                                        te.players = gamePlayers;
                                        te.boardInfo = board;
                                        te.activeCard = null;
                                        te.winner = winnerNum;
                                        te.turnPlayer = 2;

                                        // 데이터 전송 및 턴 변경
                                        SendAndTrunEnd(te, turn);

                                        break;
                                    }
                            }
                        }
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
            turn = 1;
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
