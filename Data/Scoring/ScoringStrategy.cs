using GrooprWASM.Model;

namespace GrooprWASM.Data
{
    public interface IScoringStrategy
    {
        public float Calculate(Student a, Student b);
    }
}