using System.Collections.Generic;
using GrooprWASM.Model;

namespace GrooprWASM.Data.Settings
{
    public class SettingsContainer
    {
        public List<Student> Students { get; set; } = new List<Student>();

        public List<List<int>> GroupOptions { get; set; } = new List<List<int>>();
    }
}