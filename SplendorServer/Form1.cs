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
        private byte[] sendBuffer = new byte[Constant.PACKET_SIZE];
        private byte[] readBuffer = new byte[Constant.PACKET_SIZE];

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
        Board board = null;                         // 현재 보드 상태
        ActiveCard activeCard;                      // 카드 활성화 상태
        public int turn = 1;                        // 현재 턴을 저장
        public bool winnerStatus = false;           // 승리 종료 조건

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

            for (int i = 0; i < Constant.PACKET_SIZE; i++)
            {
                sendBuffer[i] = 0;
            }
        }

        public void ReadStream(int num)
        {
            if (num == 1)
            {
                WriteLog("플레이어 1 턴");
                m_stream1.Read(readBuffer, 0, Constant.PACKET_SIZE);
            }
            else if (num == 2)
            {
                WriteLog("플레이어 2 턴");
                m_stream2.Read(readBuffer, 0, Constant.PACKET_SIZE);
            }
        }

        public void SendAndTrunEnd(TurnEnd te)
        {
            if (turn == 1)
            {
                // Player 1
                te.turnPlayer = 2;
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
                te.turnPlayer = 1;

                Packet.Serialize(te).CopyTo(sendBuffer, 0);
                Send(2);

                // 상대방 플레이어 카드 활성화 전송
                te.activeCard = activeCard;
                Packet.Serialize(te).CopyTo(sendBuffer, 0);
                Send(1);

                turn = 1;
            }
            WriteLog("");
        }

        public void GameInit()
        {
            /*
             * 1. 클라이언트에게 보드 정보, 플레이어 초기 정보 (Turn End 타입) 전송
             *    (Player1 Send) (Player2 Send)
             */
            board = new Board();
            gamePlayers[0] = new Player();
            gamePlayers[1] = new Player();
            activeCard = new ActiveCard();

            // 치트키
            for (int i = 0; i < 5; i++)
            {
                gamePlayers[0].gemSale[i] = 3;
                gamePlayers[1].gemSale[i] = 3;
            }

            m_TurnEnd = new TurnEnd();
            m_TurnEnd.Type = (int)PacketType.turnEnd;

            m_TurnEnd.chosenGems = null;
            m_TurnEnd.chosenCardID = -1;
            m_TurnEnd.chosenDeck = -1;
            m_TurnEnd.chosenNobleID = -1;
            m_TurnEnd.players = gamePlayers;
            m_TurnEnd.boardInfo = board;
            m_TurnEnd.activeCard = activeCard;
            m_TurnEnd.winner = 0;
            m_TurnEnd.turnPlayer = 1;     // 플레이어1 먼저 수행하도록

            //WriteLog("Player1, Player2 - 초기 보드 정보, 플레이어 정보 전송");
            WriteLog("=============== 게임시작 ===============");
            WriteLog("");

            Packet.Serialize(m_TurnEnd).CopyTo(sendBuffer, 0);
            Send(1);
            Packet.Serialize(m_TurnEnd).CopyTo(sendBuffer, 0);
            Send(2);
        }

        public bool GemIsValid()
        {
            int gemCnt = 0;
            int twoGemCnt = 0;

            for (int i = 0; i < 5; i++)
            {
                if (m_GemClass.gems[i] == 1)
                {
                    gemCnt++;
                }
                // 하나의 보석을 두 개 가져온 경우
                else if (m_GemClass.gems[i] == 2)
                {
                    if (board.boardGems[i] < 4)
                    {
                        // 위배 - 보석이 4개 이하인 경우
                        WriteLog("보드에 보석이 4개 이하이므로 2개를 가져올 수 없음");
                        return false;
                    }
                    else
                    {
                        twoGemCnt++;
                    }
                }
                // 위배 - 하나의 보석을 세 개 이상 가져온 경우
                else if (m_GemClass.gems[i] > 2)
                {
                    //WriteLog("보석 유효하지 않음. [하나의 보석을 3개 가져옴]");
                    return false;
                }
            }

            /*
            WriteLog("gemCnt : " + gemCnt);
            WriteLog("twoGemCnt : " + twoGemCnt);
            */

            if ((gemCnt > 0) && (gemCnt <= 3) && (twoGemCnt == 0)) // 변경
            {
                // 유효한 케이스 1
                for (int i = 0; i < 5; i++)
                {
                    // 현재 보드에서 보석을 가져올 수 있는지 검사
                    if ((m_GemClass.gems[i]) == 1 && (board.boardGems[i] < 1))
                    {
                        WriteLog("보드의 보석이 부족");
                        return false;
                    }
                }
                return true;
            }
            else if ((gemCnt == 0) && (twoGemCnt == 1))
            {
                // 유효한 케이스 2
                for (int i = 0; i < 5; i++)
                {
                    // 현재 보드에서 보석을 가져올 수 있는지 검사
                    if ((m_GemClass.gems[i]) == 2 && (board.boardGems[i] < 2))
                    {
                        WriteLog("보드의 보석이 부족");
                        return false;
                    }
                }
                return true;
            }
            else
                return false;
        }

        void CardActivate()
        {
            activeCard = new ActiveCard();
            int num = 0;

            if (turn == 1)
            {
                num = 1;
                //WriteLog("== Player2의 활성화될 카드 계산 ==");
            }

            else if (turn == 2)
            {
                num = 0;
                //WriteLog("== Player1의 활성화될 카드 계산 ==");
            }

            int[] currentPlayerGem = new int[5];

            // 현재 플레이어의 보석과 할인 받을 수 있는 보석을 계산해 가져오기
            for (int i = 0; i < 5; i++)
            {
                currentPlayerGem[i] = gamePlayers[num].gemSale[i] + gamePlayers[num].playerGems[i];
            }
            //WriteLog("현재 보석 현황 (보유 보석 + 할인 보석)");
            //WriteLog(currentPlayerGem[0] + " " + currentPlayerGem[1] + " " + currentPlayerGem[2] + " " + currentPlayerGem[3] + " " + currentPlayerGem[4]);

            // i : 카드 변수, j : 잼 변수
            for (int i = 0; i < 4; i++)
            {
                bool isActiveOne = true;
                bool isActiveTwo = true;
                bool isActiveThree = true;

                for (int j = 0; j < 5; j++)
                {
                    //WriteLog("카드 1-" + i + "의 보석 " + j + " : " + board.boardCards1[i].cardCost[j] + " vs " + currentPlayerGem[j] + " : " + j + " vs 플레이어 보석 ");
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
                    //WriteLog("카드 2-" + i + "의 보석 " + j + " : " + board.boardCards1[i].cardCost[j] + " vs " + currentPlayerGem[j] + " : " + j + " vs 플레이어 보석 ");
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
                    //WriteLog("카드 3-" + i + "의 보석 " + j + " : " + board.boardCards1[i].cardCost[j] + " vs " + currentPlayerGem[j] + " : " + j + " vs 플레이어 보석 ");
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

            // 로그 출력
            string activeLog = "";
            for (int i = 0; i < 4; i++)
                activeLog += activeCard.activeCards3[i].ToString() + " ";
            //WriteLog(activeLog);
            activeLog = "";

            for (int i = 0; i < 4; i++)
                activeLog += activeCard.activeCards2[i].ToString() + " ";
            //WriteLog(activeLog);
            activeLog = "";

            for (int i = 0; i < 4; i++)
                activeLog += activeCard.activeCards1[i].ToString() + " ";
            //WriteLog(activeLog);
            //WriteLog("== 활성화될 카드 계산 종료 ==");
        }

        int CheckNoble(int playerNum)
        {
            //WriteLog("===== 귀족 방문 가능 여부 검사 =====");
            int index = 0;
            //WriteLog("보드에 있는 귀족" + board.boardNoble[0].nobleID + ", " + board.boardNoble[1].nobleID + ", " + board.boardNoble[2].nobleID + ", " + board.boardNoble[3].nobleID + ", " + board.boardNoble[4].nobleID);
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
                    int sendNobleID = chkNoble.nobleID;
                    // 구매 가능한 경우
                    WriteLog("보드의 " + index + "번째, ID : " + chkNoble.nobleID + "인 귀족 가져옴");
                    // 플레이어와 보드의 귀족카드를 업데이트
                    gamePlayers[playerNum].playerNoble.Add(chkNoble);
                    gamePlayers[playerNum].totalScore += 3;
                    //WriteLog("플레이어에 귀족카드, 점수 추가");
                    if (board.deckNoble.Count != 0)
                    {
                        board.DrawCard(4);
                        //WriteLog("보드에 있는 귀족" + board.boardNoble[0].nobleID + ", " + board.boardNoble[1].nobleID + ", " + board.boardNoble[2].nobleID + ", " + board.boardNoble[3].nobleID + ", " + board.boardNoble[4].nobleID + ", " + board.boardNoble[5].nobleID);
                        RemoveAndAddNoble(board.boardNoble, index);
                    }
                    else
                    {
                        board.boardNoble[index].nobleID = 0;                        
                    }
                    //WriteLog("보드 귀족 업데이트");
                    //WriteLog("보드에 있는 귀족" + board.boardNoble[0].nobleID + ", " + board.boardNoble[1].nobleID + ", " + board.boardNoble[2].nobleID + ", " + board.boardNoble[3].nobleID + ", " + board.boardNoble[4].nobleID);
                    return sendNobleID;
                }
                index++;
            }
            WriteLog("귀족 구매 불가");
            return -1;
        }


        int purchaseCard(SelectCard mSelectCard, int playerNum)
        {
            //WriteLog("===== 카드 구매 =====");
            int id = mSelectCard.cardId;
            WriteLog(id + "번 카드 구매");
            int level = 0;
            if (id < 41)
            {
                //WriteLog("레벨1카드 구매");
                level = 1;
            }
            else if (id < 71)
            {
                //WriteLog("레벨2카드 구매");
                level = 2;
            }
            else if (id < 91)
            {
                //WriteLog("레벨3카드 구매");
                level = 3;
            }

            // 보드에서 구매한 카드 제거, 덱에서 보드에 새로운 카드 추가
            if (level == 1) // level1
            {
                int i = 0;
                //WriteLog("보드 레벨 1 현재 카드");
                //WriteLog(board.boardCards1[0].cardID + ", " + board.boardCards1[1].cardID + ", " + board.boardCards1[2].cardID + ", " + board.boardCards1[3].cardID);
                while (id != board.boardCards1[i].cardID)
                {
                    i++;
                }
                //WriteLog("보드의 1-" + i + "번째 카드 선택");
                // 플레이어 보유 보석에서 카드 비용 제거
                for (int n = 0; n < 5; n++)
                {
                    if (board.boardCards1[i].cardCost[n] > gamePlayers[playerNum].gemSale[n])
                    {
                        gamePlayers[playerNum].playerGems[n] -=
                            (board.boardCards1[i].cardCost[n] - gamePlayers[playerNum].gemSale[n]);
                        board.boardGems[n] +=
                            (board.boardCards1[i].cardCost[n] - gamePlayers[playerNum].gemSale[n]);
                    }
                }
                //WriteLog("플레이어" + turn + "에서 레벨 1 카드 비용제거 완료");
                gamePlayers[playerNum].gemSale[board.boardCards1[i].cardGem]++;
                //WriteLog(turn + "번 플레이어 할인 보석( " + board.boardCards1[i].cardGem + " )추가");
                // 플레이어1 보유 카드 목록에 추가
                gamePlayers[playerNum].playerCards.Add(board.boardCards1[i]);
                //WriteLog(turn + "번 플레이어 카드 " + board.boardCards1[i].cardID + " 추가");
                // 플레이어 점수 증가
                gamePlayers[playerNum].totalScore += board.boardCards1[i].cardScore;
                //WriteLog(turn + "번 플레이어 점수 증가");

                board.DrawCard(1);
                //WriteLog(board.boardCards1[0].cardID + ", " + board.boardCards1[1].cardID + ", " + board.boardCards1[2].cardID + ", " + board.boardCards1[3].cardID + ", " + board.boardCards1[4].cardID);
                RemoveAndAddCard(board.boardCards1, i);
                //WriteLog("보드 레벨 1 카드 업데이트 후");
                //WriteLog(board.boardCards1[0].cardID + ", " + board.boardCards1[1].cardID + ", " + board.boardCards1[2].cardID + ", " + board.boardCards1[3].cardID);
            }
            else if (level == 2) // level2
            {
                int i = 0;
                //WriteLog("보드 레벨 2 현재 카드");
                //WriteLog(board.boardCards2[0].cardID + ", " + board.boardCards2[1].cardID + ", " + board.boardCards2[2].cardID + ", " + board.boardCards2[3].cardID);
                while (id != board.boardCards2[i].cardID)
                {
                    i++;
                }
                //WriteLog("보드의 2-" + i + "번째 카드 선택");

                // 플레이어 보유 보석에서 카드 비용 제거
                for (int n = 0; n < 5; n++)
                {
                    if (board.boardCards2[i].cardCost[n] > gamePlayers[playerNum].gemSale[n])
                    {
                        gamePlayers[playerNum].playerGems[n] -=
                            (board.boardCards2[i].cardCost[n] - gamePlayers[playerNum].gemSale[n]);
                        board.boardGems[n] +=
                            (board.boardCards2[i].cardCost[n] - gamePlayers[playerNum].gemSale[n]);
                    }
                }
                //WriteLog("플레이어" + turn + "에서 레벨 2 카드 비용제거 완료");
                gamePlayers[playerNum].gemSale[board.boardCards2[i].cardGem]++;
                //WriteLog(turn + "번 플레이어 할인 보석 추가");
                // 플레이어1 보유 카드 목록에 추가
                gamePlayers[playerNum].playerCards.Add(board.boardCards2[i]);
                //WriteLog(turn + "번 플레이어 카드 추가");
                // 플레이어 점수 증가
                gamePlayers[playerNum].totalScore += board.boardCards2[i].cardScore;
                //WriteLog(turn + "번 플레이어 점수 증가");

                board.DrawCard(2);
                RemoveAndAddCard(board.boardCards2, i);
                //WriteLog("보드 레벨 2 카드 업데이트 후");
                //WriteLog(board.boardCards2[0].cardID + ", " + board.boardCards2[1].cardID + ", " + board.boardCards2[2].cardID + ", " + board.boardCards2[3].cardID);
            }
            else if (level == 3) // level3
            {
                int i = 0;
                //WriteLog("보드 레벨 3 현재 카드");
                //WriteLog(board.boardCards3[0].cardID + ", " + board.boardCards3[1].cardID + ", " + board.boardCards3[2].cardID + ", " + board.boardCards3[3].cardID);
                while (id != board.boardCards3[i].cardID)
                {
                    i++;
                }
                //WriteLog("보드의 2-" + i + "번째 카드 선택");

                // 플레이어 보유 보석에서 카드 비용 제거
                for (int n = 0; n < 5; n++)
                {
                    if (board.boardCards3[i].cardCost[n] > gamePlayers[playerNum].gemSale[n])
                    {
                        gamePlayers[playerNum].playerGems[n] -=
                            (board.boardCards3[i].cardCost[n] - gamePlayers[playerNum].gemSale[n]);
                        board.boardGems[n] +=
                            (board.boardCards3[i].cardCost[n] - gamePlayers[playerNum].gemSale[n]);
                    }
                }
                //WriteLog("플레이어" + turn + "에서 레벨 3 카드 비용제거 완료");
                gamePlayers[playerNum].gemSale[board.boardCards3[i].cardGem]++;
                //WriteLog(turn + "번 플레이어 할인 보석 추가");
                // 플레이어1 보유 카드 목록에 추가
                gamePlayers[playerNum].playerCards.Add(board.boardCards3[i]);
                //WriteLog(turn + "번 플레이어 카드 추가");
                // 플레이어 점수 증가
                gamePlayers[playerNum].totalScore += board.boardCards3[i].cardScore;
                //WriteLog(turn + "번 플레이어 점수 증가");

                board.DrawCard(3);
                RemoveAndAddCard(board.boardCards3, i);
                //WriteLog("보드 레벨 3 카드 업데이트 후");
                //WriteLog(board.boardCards3[0].cardID + ", " + board.boardCards3[1].cardID + ", " + board.boardCards3[2].cardID + ", " + board.boardCards3[3].cardID);
            }

            return id;
        }

        public void RemoveAndAddCard(List<Card> list, int toRemove)
        {
            //WriteLog("선택 카드 인덱스 : " + toRemove);
            list[toRemove] = list[4];
            //WriteLog(list[0].cardID + ", " + list[1].cardID + ", " + list[2].cardID + ", " + list[3].cardID + ", " + list[4].cardID);
            list.RemoveAt(4);
        }

        public void RemoveAndAddNoble(List<Noble> list, int toRemove)
        {
            list[toRemove] = list[5];
            //WriteLog(toRemove + "번 자리와 " + (list.Count - 1) + "번 자리의 카드를 바꿈");
            //WriteLog("보드에 있는 귀족" + board.boardNoble[0].nobleID + ", " + board.boardNoble[1].nobleID + ", " + board.boardNoble[2].nobleID + ", " + board.boardNoble[3].nobleID + ", " + board.boardNoble[4].nobleID + ", " + board.boardNoble[5].nobleID);
            list.RemoveAt(5);
            //WriteLog((list.Count - 1) + "번 자리의 카드를 제거");
            //WriteLog("보드에 있는 귀족" + board.boardNoble[0].nobleID + ", " + board.boardNoble[1].nobleID + ", " + board.boardNoble[2].nobleID + ", " + board.boardNoble[3].nobleID + ", " + board.boardNoble[4].nobleID);
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
                        return 2;
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

                InGame();
                //while (m_bStop)
                //{
                //    WriteLog("Player 접속 대기중...");

                //    Init init = new Init();

                //    // 플레이어 1 연결
                //    m_client1 = m_server.AcceptTcpClient();
                //    if (m_client1.Connected)
                //    {
                //        m_bConnect1 = true;
                //        WriteLog("Player1 접속");
                //        m_stream1 = m_client1.GetStream();

                //        init.playerNum = 1;
                //        Packet.Serialize(init).CopyTo(sendBuffer, 0);
                //        Send(1);
                //    }
                //    init.playerNum = 0;

                //    // 플레이어 2 연결
                //    m_client2 = m_server.AcceptTcpClient();
                //    if (m_client2.Connected)
                //    {
                //        m_bConnect2 = true;
                //        WriteLog("Player2 접속");
                //        m_stream2 = m_client2.GetStream();

                //        init.playerNum = 2;
                //        Packet.Serialize(init).CopyTo(sendBuffer, 0);
                //        Send(2);
                //    }

                //    // 클라이언트에게 보드 정보, 각 플레이어 초기 정보 전송
                //    GameInit();

                //    // 게임 시작
                //    while (m_bConnect1 && m_bConnect2)
                //    {
                //        // Player 1 == turn 1
                //        // Player 2 == turn 2
                //        while (turn == 1 || turn == 2)
                //        {
                //            try
                //            {   
                //                // 1 : Player1 stream
                //                // 2 : Player2 stream
                //                ReadStream(turn);
                //            }
                //            catch
                //            {
                //                WriteLog("서버에서 데이터를 읽는데 에러가 발생해 서버를 종료합니다.");
                //                ServerStop();
                //                this.Invoke(new MethodInvoker(delegate ()
                //                {
                //                    btnServer.Text = "서버켜기";
                //                    btnServer.ForeColor = Color.Black;
                //                }));
                //                return;
                //            }

                //            Packet packet = (Packet)Packet.Desserialize(readBuffer);

                //            switch ((int)packet.Type)
                //            {
                //                case (int)PacketType.gem:
                //                    {
                //                        /*
                //                         * 1. 플레이어가 선택한 보석이 유효한지 검사                    - GemIsVaild()
                //                         * 2. 1을 위배한다면 상태 값을 변경해 클라이언트에게 전송       - (Player1 Send)
                //                         *     
                //                         * 3. 서버측에서 플레이어 보유하고 있는 보석 정보를 업데이트 & 현재 보드 업데이트
                //                         * 4. 상대방 플레이어 카드 활성화 계산 수행                     - CardActivate(1)
                //                         * 5. 귀족 방문 여부 체크                                       - CheckNoble(0)
                //                         * 6. 승리 조건 검사                                            - CheckWinner(0)
                //                         * 7. 플레이어1,2 에게 정보 전송
                //                         */

                //                        m_GemClass = (Gem)Packet.Desserialize(readBuffer);
                //                        WriteLog("Player" + turn  + " - 보석 선택");

                                        
                //                        WriteLog("선택한 보석");
                //                        WriteLog(m_GemClass.gems[0].ToString() + m_GemClass.gems[1].ToString() + m_GemClass.gems[2].ToString() + m_GemClass.gems[3].ToString() + m_GemClass.gems[4].ToString());


                //                        if (!GemIsValid())
                //                        {
                //                            // 보석이 유효하지 않는 경우
                //                            WriteLog("Player" + turn + " - 보석이 유효하지 않습니다");
                //                            Gem sendInValid = new Gem();
                //                            sendInValid.Type = (int)PacketType.gem;

                //                            sendInValid.gemStatus = false;
                //                            Packet.Serialize(sendInValid).CopyTo(sendBuffer, 0);
                //                            Send(turn);
                //                            break;
                //                        }
                //                        else
                //                        {
                //                            // 보석이 유효한 경우
                //                            Gem sendValid = new Gem();
                //                            sendValid.Type = (int)PacketType.gem;

                //                            sendValid.gemStatus = true;
                //                            Packet.Serialize(sendValid).CopyTo(sendBuffer, 0);
                //                            Send(turn);
                //                        }

                //                        // 플레이어가 보유하고 있는 보석 정보를 업데이트 & 현재 보드 정보 업데이트
                //                        for (int i = 0; i < 5; i++)
                //                        {
                //                            gamePlayers[turn - 1].playerGems[i] += m_GemClass.gems[i];
                //                            board.boardGems[i] -= m_GemClass.gems[i];
                //                        }

                //                        CardActivate();

                //                        // 귀족 방문 여부 체크
                //                        int nobleID = CheckNoble(turn - 1);

                //                        // 승리 조건 검사
                //                        int winnerNum = checkWinner();

                //                        // 플레이어1,2에게 TurnEnd 패킷 전송
                //                        TurnEnd sendStatus = new TurnEnd();
                //                        sendStatus.Type = (int)PacketType.turnEnd;

                //                        sendStatus.chosenGems = m_GemClass.gems;
                //                        sendStatus.chosenCardID = -1;
                //                        sendStatus.chosenDeck = -1;
                //                        sendStatus.chosenNobleID = nobleID;
                //                        sendStatus.players = gamePlayers;
                //                        sendStatus.boardInfo = board;
                //                        sendStatus.activeCard = null;
                //                        sendStatus.winner = winnerNum;

                //                        WriteLog("Player" + turn + " - 턴 종료");

                //                        // 데이터 전송 및 턴 변경
                //                        SendAndTrunEnd(sendStatus);
                                        
                //                        break;
                //                    }
                //                case (int)PacketType.card:
                //                    {
                //                        this.m_SelectCard = (SelectCard)Packet.Desserialize(this.readBuffer);

                //                        // 로그 출력
                //                        WriteLog("Player" + turn + " - 카드 선택");

                //                        TurnEnd te = new TurnEnd();

                //                        // 카드 구매
                //                        int cardID = purchaseCard(m_SelectCard, turn - 1);

                //                        // 귀족 방문 여부 검사
                //                        int nobleID = CheckNoble(turn - 1);

                //                        // 상대방 카드 활성화 여부 함수 호출
                //                        CardActivate();

                //                        // 승리 여부 검사
                //                        int winnerNum = checkWinner();

                //                        // 선택 카드 레벨 추출
                //                        int id = m_SelectCard.cardId;
                //                        int level = 0;

                //                        if (id < 41)
                //                        {
                //                            level = 1;
                //                        }
                //                        else if (id < 71)
                //                        {
                //                            level = 2;
                //                        }
                //                        else if (id < 91)
                //                        {
                //                            level = 3;
                //                        }

                //                        // TurnEnd 클래스 정보 수정
                //                        te.chosenGems = null;
                //                        te.chosenCardID = cardID;
                //                        te.chosenDeck = level;
                //                        te.chosenNobleID = nobleID;
                //                        te.players = gamePlayers;
                //                        te.boardInfo = board;
                //                        te.activeCard = null;
                //                        te.winner = winnerNum;

                //                        // 데이터 전송 및 턴 변경
                //                        SendAndTrunEnd(te);

                //                        break;
                //                    }
                //            }

                //            if (checkWinner() != 0 && turn == 1)
                //            {
                //                // 종료 조건 만족 시
                //                WriteLog("CheckWinner");

                //                ReadStream(1);
                //                Packet packet1 = (Packet)Packet.Desserialize(readBuffer);
                //                int type1 = (int)packet1.Type;

                //                ReadStream(2);
                //                Packet packet2 = (Packet)Packet.Desserialize(readBuffer);
                //                int type2 = (int)packet2.Type;

                //                if (type1 == (int)PacketType.restart && type2 == (int)PacketType.restart)
                //                {
                //                    // 게임 재시작
                //                    WriteLog("게임 재시작");

                //                    packet1.Type = (int)PacketType.restart;
                //                    Packet.Serialize(packet1).CopyTo(sendBuffer, 0);
                //                    Send(1);
                //                    Packet.Serialize(packet1).CopyTo(sendBuffer, 0);
                //                    Send(2);
                //                    GameInit();
                //                }
                //                else if (type1 == (int)PacketType.end || type2 == (int)PacketType.end)
                //                {
                //                    // 게임 종료
                //                    WriteLog("클라이언트 종료");

                //                    packet1.Type = (int)PacketType.end;
                //                    Packet.Serialize(packet1).CopyTo(sendBuffer, 0);
                //                    Send(1);
                //                    Packet.Serialize(packet1).CopyTo(sendBuffer, 0);
                //                    Send(2);

                //                    m_bConnect1 = false;
                //                    m_bConnect2 = false;

                //                    m_stream1.Close();
                //                    m_stream2.Close();

                //                    break;
                //                }
                //            }
                //        }
                //    }
                //}
            }

            catch (Exception ex)
            {
                WriteLog(ex.Message + " 예외 발생으로 인해 서버를 종료합니다.");
                if (!m_bStop)
                    return;

                //m_bStop = false;
                //m_bConnect1 = false;
                //m_bConnect2 = false;
                //m_server.Stop();
                //m_stream1.Close();
                //m_stream2.Close();
                WriteLog("=============== 게임 종료 ===============");
                WriteLog("");
                //m_thServer.Abort();
                ServerStop();

                //ServerStop();
                //this.Invoke(new MethodInvoker(delegate ()
                //{
                //    btnServer.Text = "서버켜기";
                //    btnServer.ForeColor = Color.Black;
                //}));
                //return;
            }
        }

        public void InGame()
        {
            try {
                while (m_bStop)
                {
                    WriteLog("Player 접속 대기중...");

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
                                try
                                {
                                    ClientExit();
                                }
                                catch (Exception e) { }
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
                                        //WriteLog("Player" + turn + " - 보석 선택");


                                        WriteLog("선택한 보석");
                                        WriteLog("검: " + m_GemClass.gems[0].ToString() + "파: " + m_GemClass.gems[1].ToString() + "초: " + m_GemClass.gems[2].ToString() + "빨: " + m_GemClass.gems[3].ToString() + "흰: " + m_GemClass.gems[4].ToString());


                                        if (!GemIsValid())
                                        {
                                            // 보석이 유효하지 않는 경우
                                            //WriteLog("Player" + turn + " - 보석이 유효하지 않습니다");
                                            Gem sendInValid = new Gem();
                                            sendInValid.Type = (int)PacketType.gem;

                                            sendInValid.gemStatus = false;
                                            Packet.Serialize(sendInValid).CopyTo(sendBuffer, 0);
                                            Send(turn);
                                            break;
                                        }
                                        else
                                        {
                                            // 보석이 유효한 경우
                                            Gem sendValid = new Gem();
                                            sendValid.Type = (int)PacketType.gem;

                                            sendValid.gemStatus = true;
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

                                        // 게임 종료 조건 검사
                                        if (winnerNum != 0)
                                        {
                                            if (turn == 1)
                                            {
                                                // 게임이 진행되도록 하여 다음 턴에 끝나게 함
                                                WriteLog("현재 플레이어 1이 승리조건에 도달했으므로, 다음턴에 게임이 종료됩니다.");
                                                winnerNum = 0;
                                            }
                                            else
                                            {
                                                // player2
                                                // 변수 지정해 게임 종료를 알려줌
                                                WriteLog("*************** Winner : " + winnerNum + " ******************");
                                                winnerStatus = true;
                                            }
                                        }

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

                                        //WriteLog("Player" + turn + " - 턴 종료");

                                        // 데이터 전송 및 턴 변경
                                        SendAndTrunEnd(sendStatus);

                                        break;
                                    }
                                case (int)PacketType.card:
                                    {
                                        this.m_SelectCard = (SelectCard)Packet.Desserialize(this.readBuffer);

                                        // 로그 출력
                                        //WriteLog("Player" + turn + " - 카드 선택");

                                        TurnEnd te = new TurnEnd();

                                        // 카드 구매
                                        int cardID = purchaseCard(m_SelectCard, turn - 1);

                                        // 귀족 방문 여부 검사
                                        int nobleID = CheckNoble(turn - 1);

                                        // 상대방 카드 활성화 여부 함수 호출
                                        CardActivate();

                                        // 승리 여부 검사
                                        int winnerNum = checkWinner();

                                        // 게임 종료 조건 검사
                                        if (winnerNum != 0)
                                        {
                                            if (turn == 1)
                                            {
                                                // 게임이 진행되도록 하여 다음 턴에 끝나게 함
                                                WriteLog("현재 플레이어 1이 승리조건에 도달했으므로, 다음턴에 게임이 종료됩니다.");
                                                winnerNum = 0;
                                            }
                                            else
                                            {
                                                // player2
                                                // 변수 지정해 게임 종료를 알려줌
                                                WriteLog("*************** Winner : " + winnerNum + " ******************");
                                                winnerStatus = true;
                                            }
                                        }

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

                                        // 데이터 전송 및 턴 변경
                                        SendAndTrunEnd(te);

                                        break;
                                    }
                            }

                            if (winnerStatus)
                            {
                                // 게임 종료 처리

                                /*
                                 * 현재 클라이언트 1, 2에게 winner 정보를 보낸상태
                                 */
                                ServerStop();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // 기다리는 클라이언트에게 연결 종료를 알려줌
                ClientExit();
            }
        }

        public void ClientExit()
        {
            WriteLog("클라이언트 종료 예외 발생");
            SendEnd();
            ServerStop();
        }

        public void SendEnd()
        {
            if (turn == 1)
            {
                // Player 1
                m_TurnEnd.winner = 2;
                WriteLog("플레이어2 에게 연결 종료를 알려줌");
                Packet.Serialize(m_TurnEnd).CopyTo(sendBuffer, 0);
                Send(2);
            }
            else if (turn == 2)
            {
                // Player 2
                m_TurnEnd.winner = 1;
                WriteLog("플레이어1 에게 연결 종료를 알려줌");
                Packet.Serialize(m_TurnEnd).CopyTo(sendBuffer, 0);
                Send(1);
            }
            WriteLog("연결 종료 패킷 전송 완료");
        }

        public void ServerStop()
        {
            if (!m_bStop)
                return;

            //m_bStop = false;
            m_bConnect1 = false;
            m_bConnect2 = false;
            //m_server.Stop();
            m_stream1.Close();
            m_stream2.Close();
            //m_thServer.Abort();
            turn = 1;
            winnerStatus = false;
            WriteLog("=============== 게임 종료 ===============");
            //게임 재시작
            InGame();
        }

        private void btnServer_Click(object sender, EventArgs e)
        {
            if (btnServer.Text == "서버켜기")
            {
                m_thServer = new Thread(new ThreadStart(ServerStart));
                m_thServer.Start();

                btnServer.Text = "서버끄기";
                btnServer.ForeColor = Color.Red;
                txtLog.Clear();
            }
            else
            {
                m_bConnect1 = false;
                m_bConnect2 = false;
                m_stream1.Close();
                m_stream2.Close();
                turn = 1;
                m_bStop = false;
                m_server.Stop();
                m_thServer.Abort();
                btnServer.Text = "서버켜기";
                btnServer.ForeColor = Color.Black;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetLocalIP();
            txtIP.Text = IP;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_server != null)
                m_server.Stop();
            if (m_thServer != null)
                m_thServer.Abort();
        }
    }
}
