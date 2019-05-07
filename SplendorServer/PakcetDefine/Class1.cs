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
        gem = 0,
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
        public int Length;
        public int Type;

        public Packet()
        {
            this.Length = 0;
            this.Type = 0;
        }

        public static byte[] Serialize(Object o)
        {
            MemoryStream ms = new MemoryStream(1024 * 4);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return ms.ToArray();
        }

        public static Object Desserialize(byte[] bt)
        {
            MemoryStream ms = new MemoryStream(1024 * 4);
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

    /* 보석 선택 시 */
    [Serializable]
    public class Gem : Packet
    {
        public int[] gems = new int[5];            // 선택된 보석
        public Player[] players = new Player[2];   // 플레이어 1, 2 정보
        public ActiveCard activeCard;              // 활성화될 카드 정보
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
        public Noble chosenNoble;                  // 방문한 귀족
        public Player[] players = new Player[2];   // 플레이어 1, 2 정보
        public Board boardInfo;                    // 보드 정보
        public ActiveCard activeCard;              // 활성화될 카드 정보
    }

}
