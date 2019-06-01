using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClassDefine
{
    /*
     * 0 : 초콜렛(검)
     * 1 : 사파이어(파)
     * 2 : 에메랄드(초)
     * 3 : 루비(빨)
     * 4 : 다이아(흰)
     * 
     *   <이전 순서>
        0 : 초콜렛(검)
        1 : 다이아(흰)
        2 : 루비(빨)
        3 : 사파이어(파)
        4 : 에메랄드(초)
    */
    [Serializable]
    public class Card
    {
        public int cardID;                     // 카드 식별자
        public int[] cardCost = new int[5];    // 구매 시 필요한 보석 개수
        public int cardScore;                  // 카드 점수
        public int cardLevel;                  // 카드 레벨
        public int cardGem;                    // 카드 보석(할인)
        public bool active;                    // 카드 활성화 정보

        public Card(int id, int cost0, int cost1, int cost2, int cost3, int cost4, int score, int level, int gem)
        {
            cardID = id;
            cardCost[0] = cost0;
            cardCost[1] = cost1;
            cardCost[2] = cost2;
            cardCost[3] = cost3;
            cardCost[4] = cost4;
            cardScore = score;
            cardLevel = level;
            cardGem = gem;
        }
    }

    [Serializable]
    public class Noble
    {
        public int nobleID;                           // 귀족카드 식별자
        public int[] nobleCost = new int[5];          // 카드 보석(비용)

        public Noble(int id, int cost0, int cost1, int cost2, int cost3, int cost4)
        {
            nobleID = id;
            nobleCost[0] = cost0;
            nobleCost[1] = cost1;
            nobleCost[2] = cost2;
            nobleCost[3] = cost3;
            nobleCost[4] = cost4;
        }
    }

    [Serializable]
    public class Player
    {
        public int totalScore;                 // 총점수
        public int[] gemSale = new int[5];     // 할인 받을 수 있는 보석 개수
        public List<Card> playerCards = new List<Card>();      // 보유하고 있는 카드
        public List<Noble> playerNoble = new List<Noble>();    // 보유하고 있는 귀족
        public int[] playerGems = new int[5];               // 보유하고 있는 보석

        public Player()
        {
            totalScore = 0;
            for(int i = 0; i < 5; i++)
            {
                gemSale[i] = 0;
                playerGems[i] = 0;
            }
        }
    }

    [Serializable]
    public class ActiveCard
    {
        public bool[] activeCards1 = { false, false, false, false };
        public bool[] activeCards2 = { false, false, false, false };
        public bool[] activeCards3 = { false, false, false, false };
    }

    [Serializable]
    public class Board
    {
        public List<Card> boardCards1 = new List<Card>();      // 보드에 있는 레벨1 카드 (4개)
        public List<Card> boardCards2 = new List<Card>();      // 보드에 있는 레벨2 카드 (4개)
        public List<Card> boardCards3 = new List<Card>();      // 보드에 있는 레벨3 카드 (4개)
        public List<Noble> boardNoble = new List<Noble>();    // 보드에 있는 귀족 (5개)
        public int[] boardGems = new int[5];                   // 보드에 있는 보석

        public List<Card> deckCards1 = new List<Card>();      // 덱에 있는 레벨1 카드 (40장)
        public List<Card> deckCards2 = new List<Card>();      // 덱에 있는 레벨2 카드 (30장)
        public List<Card> deckCards3 = new List<Card>();      // 덱에 있는 레벨3 카드 (20장)
        public List<Noble> deckNoble = new List<Noble>();    // 덱에 있는 귀족 (10개)

        Random rnd = new Random();

        public Board()
        {
            boardGems[0] = boardGems[1] = boardGems[2] = boardGems[3] = boardGems[4] = 4;   // 보석 초기화
            CardInit(); // 카드 생성 및 덱 리스트에 추가

            // 보드 카드 배치 
            for(int i = 0; i < 4; i++)
            {
                DrawCard(1);
                DrawCard(2);
                DrawCard(3);
                DrawCard(4);
            }
            DrawCard(4);
        }

        public void DrawCard(int level)
        {
            int num;
            if(level == 1)
            {
                num = rnd.Next(deckCards1.Count);
                boardCards1.Add(deckCards1[num]);
                deckCards1.Remove(deckCards1[num]);
            }
            else if(level == 2)
            {
                num = rnd.Next(deckCards2.Count);
                boardCards2.Add(deckCards2[num]);
                deckCards2.Remove(deckCards2[num]);
            }
            else if(level == 3)
            {
                num = rnd.Next(deckCards3.Count);
                boardCards3.Add(deckCards3[num]);
                deckCards3.Remove(deckCards3[num]);
            }
            else if(level == 4)
            {
                num = rnd.Next(deckNoble.Count);
                boardNoble.Add(deckNoble[num]);
                deckNoble.Remove(deckNoble[num]);
            }
        }

        public void CardInit()
        {
            int cardID = 1;

            // Level 1
            deckCards1.Add(new Card(cardID++, 0, 1, 1, 1, 1, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 0, 0, 1, 0, 2, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 0, 2, 0, 0, 2, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 1, 0, 3, 0, 1, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 0, 0, 0, 0, 3, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 0, 1, 1, 2, 1, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 0, 2, 1, 2, 0, 0, 1, 0));
            deckCards1.Add(new Card(cardID++, 0, 0, 0, 4, 0, 1, 1, 0));

            deckCards1.Add(new Card(cardID++, 2, 1, 0, 0, 0, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 1, 1, 2, 0, 1, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 1, 1, 1, 0, 1, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 0, 0, 1, 1, 3, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 3, 0, 0, 0, 0, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 0, 1, 2, 0, 2, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 2, 0, 0, 0, 2, 0, 1, 4));
            deckCards1.Add(new Card(cardID++, 0, 0, 4, 0, 0, 1, 1, 4));

            deckCards1.Add(new Card(cardID++, 0, 2, 0, 1, 0, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 0, 0, 2, 2, 0, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 0, 1, 0, 3, 1, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 1, 1, 1, 1, 0, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 2, 1, 1, 1, 0, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 2, 0, 2, 1, 0, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 0, 0, 3, 0, 0, 0, 1, 3));
            deckCards1.Add(new Card(cardID++, 4, 0, 0, 0, 0, 1, 1, 3));

            deckCards1.Add(new Card(cardID++, 0, 3, 0, 0, 0, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 3, 1, 1, 0, 0, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 0, 0, 0, 2, 1, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 2, 2, 0, 0, 1, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 1, 2, 0, 1, 1, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 1, 1, 0, 1, 1, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 0, 2, 2, 0, 0, 0, 1, 1));
            deckCards1.Add(new Card(cardID++, 0, 4, 0, 0, 0, 1, 1, 1));

            deckCards1.Add(new Card(cardID++, 1, 0, 0, 2, 2, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 1, 0, 2, 0, 0, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 1, 0, 1, 1, 1, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 0, 0, 0, 3, 0, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 0, 0, 0, 2, 2, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 1, 0, 1, 1, 2, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 1, 3, 0, 1, 0, 0, 1, 2));
            deckCards1.Add(new Card(cardID++, 0, 0, 0, 0, 4, 1, 1, 2));

            // Level 2
            deckCards2.Add(new Card(cardID++, 0, 3, 0, 2, 2, 1, 2, 0)); // 41
            deckCards2.Add(new Card(cardID++, 2, 3, 0, 0, 3, 1, 2, 0));
            deckCards2.Add(new Card(cardID++, 0, 0, 2, 1, 4, 2, 2, 0));
            deckCards2.Add(new Card(cardID++, 0, 5, 0, 0, 0, 2, 2, 0));
            deckCards2.Add(new Card(cardID++, 0, 0, 3, 0, 5, 2, 2, 0));
            deckCards2.Add(new Card(cardID++, 6, 0, 0, 0, 0, 3, 2, 0));

            deckCards2.Add(new Card(cardID++, 0, 0, 3, 2, 2, 1, 2, 4));
            deckCards2.Add(new Card(cardID++, 3, 0, 0, 2, 3, 1, 2, 4));
            deckCards2.Add(new Card(cardID++, 0, 5, 0, 3, 0, 2, 2, 4));
            deckCards2.Add(new Card(cardID++, 0, 0, 0, 5, 0, 2, 2, 4));
            deckCards2.Add(new Card(cardID++, 4, 2, 1, 0, 0, 2, 2, 4));
            deckCards2.Add(new Card(cardID++, 0, 0, 0, 6, 0, 3, 2, 4));

            deckCards2.Add(new Card(cardID++, 0, 3, 3, 0, 2, 1, 2, 3));
            deckCards2.Add(new Card(cardID++, 2, 2, 0, 3, 0, 1, 2, 3));
            deckCards2.Add(new Card(cardID++, 1, 4, 0, 2, 0, 2, 2, 3));
            deckCards2.Add(new Card(cardID++, 0, 0, 0, 0, 5, 2, 2, 3));
            deckCards2.Add(new Card(cardID++, 0, 0, 0, 5, 3, 2, 2, 3));
            deckCards2.Add(new Card(cardID++, 0, 0, 0, 0, 6, 3, 2, 3));

            deckCards2.Add(new Card(cardID++, 3, 0, 2, 3, 0, 1, 2, 1));
            deckCards2.Add(new Card(cardID++, 3, 2, 2, 0, 0, 1, 2, 1));
            deckCards2.Add(new Card(cardID++, 0, 1, 0, 4, 2, 2, 2, 1));
            deckCards2.Add(new Card(cardID++, 5, 3, 0, 0, 0, 2, 2, 1));
            deckCards2.Add(new Card(cardID++, 5, 0, 0, 0, 0, 2, 2, 1));
            deckCards2.Add(new Card(cardID++, 0, 0, 6, 0, 0, 3, 2, 1));

            deckCards2.Add(new Card(cardID++, 2, 0, 2, 0, 3, 1, 2, 2));
            deckCards2.Add(new Card(cardID++, 0, 2, 3, 3, 0, 1, 2, 2));
            deckCards2.Add(new Card(cardID++, 2, 0, 4, 0, 1, 2, 2, 2));
            deckCards2.Add(new Card(cardID++, 0, 0, 5, 0, 0, 2, 2, 2));
            deckCards2.Add(new Card(cardID++, 3, 0, 5, 0, 0, 2, 2, 2));
            deckCards2.Add(new Card(cardID++, 0, 6, 0, 0, 0, 3, 2, 2));

            // Level 3
            deckCards3.Add(new Card(cardID++, 0, 3, 3, 3, 5, 3, 3, 0));
            deckCards3.Add(new Card(cardID++, 0, 0, 7, 0, 0, 4, 3, 0));
            deckCards3.Add(new Card(cardID++, 3, 0, 6, 0, 3, 4, 3, 0));
            deckCards3.Add(new Card(cardID++, 3, 0, 7, 0, 0, 5, 3, 0));

            deckCards3.Add(new Card(cardID++, 5, 3, 3, 0, 3, 3, 3, 4));
            deckCards3.Add(new Card(cardID++, 0, 7, 0, 0, 0, 4, 3, 4));
            deckCards3.Add(new Card(cardID++, 3, 6, 0, 3, 0, 4, 3, 4));
            deckCards3.Add(new Card(cardID++, 0, 7, 0, 3, 0, 5, 3, 4));

            deckCards3.Add(new Card(cardID++, 3, 5, 3, 3, 0, 3, 3, 3));
            deckCards3.Add(new Card(cardID++, 0, 3, 0, 6, 3, 4, 3, 3));
            deckCards3.Add(new Card(cardID++, 0, 0, 0, 7, 0, 4, 3, 3));
            deckCards3.Add(new Card(cardID++, 0, 0, 0, 7, 3, 5, 3, 3));

            deckCards3.Add(new Card(cardID++, 3, 3, 0, 5, 3, 3, 3, 1));
            deckCards3.Add(new Card(cardID++, 0, 0, 0, 0, 7, 4, 3, 1));
            deckCards3.Add(new Card(cardID++, 0, 0, 3, 3, 6, 4, 3, 1));
            deckCards3.Add(new Card(cardID++, 0, 0, 3, 0, 7, 5, 3, 1));

            deckCards3.Add(new Card(cardID++, 3, 0, 5, 3, 3, 3, 3, 2));
            deckCards3.Add(new Card(cardID++, 7, 0, 0, 0, 0, 4, 3, 2));
            deckCards3.Add(new Card(cardID++, 6, 3, 3, 0, 0, 4, 3, 2));
            deckCards3.Add(new Card(cardID++, 7, 3, 0, 0, 0, 5, 3, 2));

            deckNoble.Add(new Noble(cardID++, 0, 0, 4, 0, 4));  // 91 ~ 100
            deckNoble.Add(new Noble(cardID++, 4, 4, 0, 0, 0));
            deckNoble.Add(new Noble(cardID++, 3, 3, 3, 0, 0));
            deckNoble.Add(new Noble(cardID++, 0, 4, 0, 4, 0));
            deckNoble.Add(new Noble(cardID++, 0, 0, 0, 4, 4));
            deckNoble.Add(new Noble(cardID++, 0, 0, 3, 3, 3));
            deckNoble.Add(new Noble(cardID++, 3, 3, 0, 3, 0));
            deckNoble.Add(new Noble(cardID++, 0, 3, 0, 3, 3));
            deckNoble.Add(new Noble(cardID++, 4, 0, 4, 0, 0));
            deckNoble.Add(new Noble(cardID++, 3, 0, 3, 0, 3));
        }
    }
}
