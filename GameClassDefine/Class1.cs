using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClassDefine
{
    /*
        0 : 다이아몬드
        1 : 루비
        2 : 사파이어(마린)
        3 : 에메랄드
        4 : 오팔
        */

    public class Card
    {
        int cardID;                     // 카드 식별자
        int[] cardCost = new int[5];    // 구매 시 필요한 보석 개수
        int cardScore;                  // 카드 점수
        int cardLevel;                  // 카드 레벨
        int cardGem;                    // 카드 보석(할인)
    }

    public class Noble
    {
        int nobleID;                    // 귀족카드 식별자
        int[] nobleCost = new int[5];   // 카드 보석(비용)
        const int nobleScore = 3;       // 귀족 고정 점수
    }

    public class Player
    {
        int totalScore;                 // 총점수
        int[] gemSale = new int[5];     // 할인 받을 수 있는 보석 개수
        List<Card> playerCards = new List<Card>();      // 보유하고 있는 카드
        List<Noble> playerNoble = new List<Noble>();    // 보유하고 있는 귀족
        int[] playerGems;               // 보유하고 있는 보석

    }
}
