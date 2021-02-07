using System.Collections.Generic;

namespace GrooprWASM.Model
{
    public class GroupSet
    {
        public List<Group> Groups { get; set; } = new List<Group>();
        public float Score { get; set; }
    }
}