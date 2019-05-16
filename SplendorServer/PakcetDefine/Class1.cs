using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GameClassDefine;

namespace PacketDefine
{
    public enum PacketType
    {
        init = 0,
        gem,
        card,
        turnEnd
    }

    public enum PacketSendERROR
    {
        normal = 0,
        error
    }

    [Serializable]
    public class Packet
    {
        public const int PACKET_SIZE = 1024 * 20;
        public int Length;
        public int Type;

        public Packet()
        {
            this.Length = 0;
            this.Type = 0;
        }

        public static byte[] Serialize(Object o)
        {
            MemoryStream ms = new MemoryStream(Packet.PACKET_SIZE);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return ms.ToArray();
        }

        public static Object Desserialize(byte[] bt)
        {
            MemoryStream ms = new MemoryStream(Packet.PACKET_SIZE);
            foreach (byte b in bt)
            {
                ms.WriteByte(b);
            }

            ms.Position = 0;
            BinaryFormatter bf = new BinaryFormatter();
            Object obj = bf.Deserialize(ms);
            ms.Close();
            return obj;
        }
    }
    /* 클라이언트 접속 시 */
    [Serializable]
    public class Init : Packet
    {
        public int playerNum;
        public Init()
        {
            playerNum = 0;
        }
    }

    /* 보석 선택 시 */
    [Serializable]
    public class Gem : Packet
    {
        public int[] gems = new int[5];            // 선택된 보석
        public Player[] players = new Player[2];   // 플레이어 1, 2 정보
        public ActiveCard activeCard;              // 활성화될 카드 정보
        public Board boardInfo;                    // 보드 정보
        public bool[] gemStatus = new bool[2]; // gemStatus[0] : true (이미 보석 선택) / gemStatus[1] : true (유효하지 않은 보석)

        public Gem()
        {
            gemStatus[0] = gemStatus[1] = false;
            activeCard = null;
        }
    }

    /* 카드 구매 시 */
    [Serializable]
    public class SelectCard : Packet
    {
        public Card chosenCard;                    // 구매한 카드
        public Player[] players = new Player[2];   // 플레이어 1, 2 정보
    }

    /* 턴 종료 시 + 초기 화면 설정 */
    [Serializable]
    public class TurnEnd : Packet
    {
        public int chosenNobleID;                  // 방문한 귀족
        public Player[] players = new Player[2];   // 플레이어 1, 2 정보
        public Board boardInfo;                    // 보드 정보
        public ActiveCard activeCard;              // 활성화될 카드 정보
        public string winner;                      // 게임 종료 시 승리자 정보
    }

}
