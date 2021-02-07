using System.Linq;
using GrooprWASM.Model;

namespace GrooprWASM.Data.Scoring
{
    public class ScoringCalculator
    {
        public void ApplyScore(GroupSet gs)
        {
            float score = 0;
            foreach (Group group in gs.Groups)
            {
                score += CalcGroupScore(group);
            }

            gs.Score = score;
        }

        private float CalcGroupScore(Group group)
        {
            float score = 0;
            foreach (Student a in group.Members)
            {
                string[] aPlus = a.PlusWishes?.Split("\n");
                string[] aMinus = a.MinusWishes?.Split("\n");
                
                // TODO someday implement list of interfaces  with validation strategies to loop through, so you can set up different point appliers through ui.
                foreach (Student b in group.Members)
                {
                    if (a.Equals(b)) continue;

                    // on way plus
                    if (aPlus != null && aPlus.Contains(b.StudNum))
                    {
                        score += 0.5f;
                    }

                    // one way minus
                    if (aMinus != null && aMinus.Contains(b.StudNum))
                    {
                        score -= 0.5f;
                    }
                    
                    // two way plus, it gets counted twice
                    if (aPlus != null && aPlus.Contains(b.StudNum) && b.PlusWishes != null && b.PlusWishes.Split("\n").Contains(a.StudNum))
                    {
                        score += 0.5f;
                    }
                    
                    // two way minus, it gets counted twice
                    if (aMinus != null && aMinus.Contains(b.StudNum) && b.MinusWishes != null && b.MinusWishes.Split("\n").Contains(a.StudNum))
                    {
                        score -= 1;
                    }
                }                
            }

            group.Score = score;
            return score;
        }
    }
}