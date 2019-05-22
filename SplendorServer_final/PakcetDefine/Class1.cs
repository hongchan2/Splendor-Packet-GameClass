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
        turnEnd,
        restart,
        end
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
            MemoryStream ms = new MemoryStream(1024 * 20);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            return ms.ToArray();
        }

        public static Object Desserialize(byte[] bt)
        {
            MemoryStream ms = new MemoryStream(1024 * 20);
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
        public bool gemStatus;                     // true = 유효하지 않은 값

        public Gem()
        {
            gemStatus = true;
        }
    }

    /* 카드 구매 시 */
    [Serializable]
    public class SelectCard : Packet
    {
        public int cardId;                    // 구매한 카드
    }

    /* 턴 종료 시 + 초기 화면 설정 */
    [Serializable]
    public class TurnEnd : Packet
    {
        public int[] chosenGems = new int[5];      // 가져간 보석 (상대방 애니메이션 효과를 위해)
        public int chosenCardID;                   // 가져간 카드 (상대방 애니메이션 효과를 위해)
        public int chosenDeck;                     // 가져간 카드 레벨 (상대방 애니메이션 효과를 위해)
        public int chosenNobleID;                  // 방문한 귀족 (애니메이션 효과를 위해)
        public Player[] players = new Player[2];   // 플레이어 1, 2 정보
        public Board boardInfo;                    // 보드 정보
        public ActiveCard activeCard;              // 활성화될 카드 정보
        public int winner;                         // 0 : 게임 진행 / 1 : Player1 승리 / 2 : Player2 승리
        public int turnPlayer;                     // 1 : Player1 / 2 : Player2
    }
}
