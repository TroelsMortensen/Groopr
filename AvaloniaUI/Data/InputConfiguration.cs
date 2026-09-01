using System.Collections.Generic;
using AvaloniaUI.Data.ScoringConfiguration;
using Logic.Models;

namespace AvaloniaUI.Data;

public class InputConfiguration
{
    public StudentList? StudentList { get; set; }
    public GroupSizeDistribution? GroupSizeDistribution { get; set; }
    public List<ScorerConfiguration> EnabledScorers { get; set; } = [];
}
