using System.Collections.Generic;

namespace GrooprWASM.Model
{
    public class Group
    {
        public List<Student> Members { get; set; } = new List<Student>();
        public int GroupNum { get; set; }
        public float Score { get; set; }
    }
}