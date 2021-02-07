using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GrooprWASM.Data.Scoring;
using GrooprWASM.Data.Settings;
using GrooprWASM.Model;

namespace GrooprWASM.Data.GroupGeneration
{
    public class RandomGroupsetStrategy : IGroupGenerationStrategy
    {
        private bool shouldStop;
        private readonly ScoringCalculator scoringCalculator = new();
        private readonly List<GroupSet> groupSets = new();

        public async Task StartAsync(Action<List<GroupSet>, int, int> myAction, SettingsContainer sc)
        {
            List<Student> students = new List<Student>(sc.Students);
            int maxIterations = 10000000;
            int updateForNumOfIterations = 100;
            Stopwatch sw = new();
            sw.Start();
            int duration = 0;
            for (int i = 0; i < maxIterations; i++)
            {
                Shuffle(students);
                foreach (List<int> option in sc.GroupOptions)
                {
                    GroupSet gs = GenerateGroupSet(students, option);
                    scoringCalculator.ApplyScore(gs);
                    InsertIntoList(gs);
                }

                if (i % updateForNumOfIterations == 0)
                {
                    duration = (int)(sw.ElapsedMilliseconds / 1000f);
                    myAction.Invoke(groupSets, i, duration);
                    await Task.Delay(1);
                }

                if (shouldStop) return;
            }

            duration = (int) (sw.ElapsedMilliseconds / 1000f);
            myAction.Invoke(groupSets, maxIterations, duration);

        }

        private void InsertIntoList(GroupSet gs)
        {
            if (groupSets.Count == 0)
            {
                groupSets.Add(gs);
                return;
            }

            // find where to insert, largest score first
            int i = 0;
            for (; i < groupSets.Count; i++)
            {
                if (gs.Score > groupSets[i].Score)
                {
                    groupSets.Insert(i, gs);
                    break;
                }
            }

            if (groupSets.Count <= 5) return;

            // find out what to throw out of the list

            float previousScore = groupSets[0].Score;
            float currentScore = -1;
            for (int j = 1; j < groupSets.Count; j++)
            {
                currentScore = groupSets[j].Score;
                if (j > 5)
                {
                    if (currentScore != previousScore)
                    {
                        if (groupSets.Count > j + 1)
                        {
                            groupSets.RemoveRange(j, groupSets.Count - j);
                        }
                    }
                }

                previousScore = currentScore;
            }
        }

        private GroupSet GenerateGroupSet(List<Student> students, List<int> option)
        {
            GroupSet gs = new();
            int idx = 0;
            int groupNum = 1;
            foreach (int numOfStudents in option)
            {
                Group g = new Group
                {
                    GroupNum = groupNum
                };

                for (int i = 0; i < numOfStudents; i++)
                {
                    g.Members.Add(students[i + idx]);
                }

                idx += numOfStudents;
                gs.Groups.Add(g);
                groupNum++;
            }

            return gs;
        }

        public void Stop()
        {
            shouldStop = true;
        }

        private static Random rng = new Random();

        public void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}