using System.Collections.Generic;

namespace Lobby.Jigsaw
{
    class LevelOfNormalStar
    {
        public int level;
        public int[] targetCounts;

        public LevelOfNormalStar(int level, List<int> targetCounts)
        {
            this.level = level;
            this.targetCounts = targetCounts.ToArray();
        }
    }

    class LevelOfFantasyStar
    {
        public int level;
        public int targetCount;

        public LevelOfFantasyStar(int level, int targetCount)
        {
            this.level = level;
            this.targetCount = targetCount;
        }
    }

    public class FantasyWheelDataProvider
    {
        List<LevelOfNormalStar> normalStars;
        List<LevelOfFantasyStar> fantasyStars;

        public FantasyWheelDataProvider()
        {
            initNormalStars();
            initFantasyStars();
        }

        public int[] getNormarStarConditions(int level)
        {
            for (int i = 0; i < normalStars.Count; i++)
            {
                var data = normalStars[i];
                if (level >= data.level)
                {
                    return data.targetCounts;
                }
            }
            return new int[] { 1, 1, 1 };
        }

        public int getFantasyTargetCount(int level)
        {
            for (int i = 0; i < fantasyStars.Count; i++)
            {
                var data = fantasyStars[i];
                if (level >= data.level)
                {
                    return data.targetCount;
                }
            }
            return 0;
        }

        void initNormalStars()
        {
            normalStars = new List<LevelOfNormalStar>();
            normalStars.Add(new LevelOfNormalStar(90001, new List<int>() { 176, 352, 880 }));
            normalStars.Add(new LevelOfNormalStar(80001, new List<int>() { 158, 316, 790 }));
            normalStars.Add(new LevelOfNormalStar(70001, new List<int>() { 141, 282, 705 }));
            normalStars.Add(new LevelOfNormalStar(60001, new List<int>() { 125, 250, 625 }));
            normalStars.Add(new LevelOfNormalStar(50001, new List<int>() { 110, 220, 550 }));
            normalStars.Add(new LevelOfNormalStar(40001, new List<int>() { 96, 192, 480 }));
            normalStars.Add(new LevelOfNormalStar(30001, new List<int>() { 83, 166, 415 }));
            normalStars.Add(new LevelOfNormalStar(20001, new List<int>() { 71, 142, 355 }));
            normalStars.Add(new LevelOfNormalStar(10001, new List<int>() { 60, 120, 300 }));
            normalStars.Add(new LevelOfNormalStar(8001, new List<int>() { 50, 100, 250 }));
            normalStars.Add(new LevelOfNormalStar(6001, new List<int>() { 41, 82, 205 }));
            normalStars.Add(new LevelOfNormalStar(4001, new List<int>() { 33, 66, 165 }));
            normalStars.Add(new LevelOfNormalStar(2001, new List<int>() { 26, 52, 130 }));
            normalStars.Add(new LevelOfNormalStar(1001, new List<int>() { 20, 40, 100 }));
            normalStars.Add(new LevelOfNormalStar(1, new List<int>() { 15, 30, 75 }));
        }

        void initFantasyStars()
        {
            fantasyStars = new List<LevelOfFantasyStar>();
            fantasyStars.Add(new LevelOfFantasyStar(90001, 17));
            fantasyStars.Add(new LevelOfFantasyStar(80001, 16));
            fantasyStars.Add(new LevelOfFantasyStar(70001, 15));
            fantasyStars.Add(new LevelOfFantasyStar(60001, 14));
            fantasyStars.Add(new LevelOfFantasyStar(50001, 13));
            fantasyStars.Add(new LevelOfFantasyStar(40001, 12));
            fantasyStars.Add(new LevelOfFantasyStar(30001, 11));
            fantasyStars.Add(new LevelOfFantasyStar(20001, 10));
            fantasyStars.Add(new LevelOfFantasyStar(10001, 9));
            fantasyStars.Add(new LevelOfFantasyStar(8001, 8));
            fantasyStars.Add(new LevelOfFantasyStar(6001, 7));
            fantasyStars.Add(new LevelOfFantasyStar(4001, 6));
            fantasyStars.Add(new LevelOfFantasyStar(2001, 5));
            fantasyStars.Add(new LevelOfFantasyStar(1001, 4));
            fantasyStars.Add(new LevelOfFantasyStar(1, 3));
        }
    }
}
