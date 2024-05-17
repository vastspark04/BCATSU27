using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BDLearningBot
{

    public class LearningBot
    {
        private class Behavior
        {
            public int behaviorID;

            public int chance;
        }

        private Dictionary<int, List<Behavior>> database;

        private int numBehaviors;

        private int startingChance;

        private int maxChance;

        private int minChance;

        public bool diminishingAdjustment;

        private bool inSession;

        private List<Behavior> sessionBehaviors;

        private Random r = new Random(1337);

        public List<float> GetCertaintyMap()
        {
            float num = (maxChance + (numBehaviors - 1) * minChance) / numBehaviors;
            List<float> list = new List<float>(database.Count);
            foreach (KeyValuePair<int, List<Behavior>> item2 in database)
            {
                float num2 = 0f;
                float num3 = 0f;
                foreach (Behavior item3 in item2.Value)
                {
                    num2 += (float)item3.chance;
                    num3 = Math.Max(num3, item3.chance);
                }
                float num4 = num2 / (float)numBehaviors;
                float item = (num3 - num4) / num;
                list.Add(item);
            }
            return list;
        }

        public LearningBot(int startingChance, int numBehaviors, int maxChance = -1, int minChance = 1)
        {
            this.numBehaviors = numBehaviors;
            this.startingChance = startingChance;
            database = new Dictionary<int, List<Behavior>>();
            if (maxChance <= 0)
            {
                this.maxChance = startingChance * 2;
            }
            else
            {
                this.maxChance = maxChance;
            }
            this.minChance = Math.Max(1, minChance);
        }

        private float GetCertainty(List<Behavior> bList, int bIdx)
        {
            float num = 0f;
            float num2 = 0f;
            foreach (Behavior b in bList)
            {
                num += (float)b.chance;
                num2 = Math.Max(num2, b.chance);
            }
            if (bIdx >= 0)
            {
                num2 = bList[bIdx].chance;
            }
            return num2 / num;
        }

        public void SaveDatabase(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<int, List<Behavior>> item in database)
            {
                int key = item.Key;
                stringBuilder.Append(key);
                stringBuilder.Append(',');
                List<Behavior> value = item.Value;
                for (int i = 0; i < value.Count; i++)
                {
                    stringBuilder.Append(value[i].chance);
                    stringBuilder.Append(',');
                }
                stringBuilder.AppendLine();
            }
            File.WriteAllText(path, stringBuilder.ToString());
        }

        public void LoadDatabase(string path)
        {
            database = new Dictionary<int, List<Behavior>>();
            char[] separator = new char[1] { ',' };
            foreach (string item2 in File.ReadLines(path))
            {
                string[] array = item2.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                int key = int.Parse(array[0]);
                List<Behavior> list = new List<Behavior>();
                for (int i = 1; i < array.Length; i++)
                {
                    int behaviorID = i - 1;
                    int chance = int.Parse(array[i]);
                    Behavior item = new Behavior
                    {
                        behaviorID = behaviorID,
                        chance = chance
                    };
                    list.Add(item);
                }
                database.Add(key, list);
            }
        }

        public void BeginSession()
        {
            if (!inSession)
            {
                inSession = true;
                sessionBehaviors = new List<Behavior>();
            }
        }

        public int GetBehavior(int situation, out float certainty)
        {
            if (!inSession)
            {
                certainty = 0f;
                return -1;
            }
            if (!database.TryGetValue(situation, out var value))
            {
                value = CreateBehaviorList();
                database.Add(situation, value);
            }
            Behavior behavior = SelectBehavior(value, out certainty);
            sessionBehaviors.Add(behavior);
            return behavior.behaviorID;
        }

        private Behavior SelectBehavior(List<Behavior> bList, out float certainty)
        {
            int num = 0;
            for (int i = 0; i < bList.Count; i++)
            {
                num += Math.Max(0, bList[i].chance);
            }
            int randomInt = GetRandomInt(num - 1);
            int num2 = 0;
            for (int j = 0; j < bList.Count; j++)
            {
                num2 += bList[j].chance;
                if (randomInt < num2)
                {
                    certainty = GetCertainty(bList, j);
                    return bList[j];
                }
            }
            Console.Out.WriteLine("LearningBot: Error 0");
            certainty = 0f;
            return null;
        }

        private int GetRandomInt(int max)
        {
            return r.Next(max);
        }

        private List<Behavior> CreateBehaviorList()
        {
            List<Behavior> list = new List<Behavior>();
            for (int i = 0; i < numBehaviors; i++)
            {
                list.Add(new Behavior
                {
                    behaviorID = i,
                    chance = startingChance
                });
            }
            return list;
        }

        public void EndVictory(int successPoints)
        {
            if (!inSession)
            {
                return;
            }
            if (diminishingAdjustment)
            {
                DiminishingAdjustment(successPoints);
            }
            else
            {
                for (int i = 0; i < sessionBehaviors.Count; i++)
                {
                    if (sessionBehaviors[i].chance + successPoints < maxChance)
                    {
                        sessionBehaviors[i].chance += successPoints;
                    }
                    else
                    {
                        sessionBehaviors[i].chance = maxChance;
                    }
                }
            }
            inSession = false;
        }

        private void DiminishingAdjustment(int adjustment)
        {
            if (adjustment == 0)
            {
                return;
            }
            int num = Math.Sign(adjustment);
            int num2 = num * adjustment;
            for (int num3 = sessionBehaviors.Count - 1; num3 >= 0; num3--)
            {
                sessionBehaviors[num3].chance += num * num2;
                if (sessionBehaviors[num3].chance < minChance)
                {
                    sessionBehaviors[num3].chance = minChance;
                }
                else if (sessionBehaviors[num3].chance > maxChance)
                {
                    sessionBehaviors[num3].chance = maxChance;
                }
                num2 = Math.Max(0, num2 - 1);
                if (num2 == 0)
                {
                    break;
                }
            }
        }

        public void EndDefeat(int lossPoints)
        {
            if (!inSession)
            {
                return;
            }
            if (diminishingAdjustment)
            {
                DiminishingAdjustment(-lossPoints);
            }
            else
            {
                for (int i = 0; i < sessionBehaviors.Count; i++)
                {
                    if (sessionBehaviors[i].chance - lossPoints > minChance)
                    {
                        sessionBehaviors[i].chance -= lossPoints;
                    }
                    else
                    {
                        sessionBehaviors[i].chance = minChance;
                    }
                }
            }
            inSession = false;
        }
    }
}