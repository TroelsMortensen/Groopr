using BlazorUI.Data.InvalidationConfiguration;
using BlazorUI.Data.ScoringConfiguration;
using Logic.Models;

namespace BlazorUI.Data;

public class InputConfiguration
{
    public StudentList? StudentList { get; set; }
    public GroupSizeDistribution? GroupSizeDistribution { get; set; }
    public List<ScorerConfiguration> EnabledScorers { get; set; } = [];
    public List<InvalidatorConfiguration> EnabledInvalidators { get; set; } = [];
}
