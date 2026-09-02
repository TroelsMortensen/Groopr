using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupCompositionGeneration;

public partial class GroupCompositionCardViewModel
{
    public string ScoreText { get; }

    public IReadOnlyList<GroupCardViewModel> Groups { get; }

    private GroupCompositionCardViewModel(string scoreText, IReadOnlyList<GroupCardViewModel> groups)
    {
        ScoreText = scoreText;
        Groups = groups;
    }

    public static GroupCompositionCardViewModel FromModel(GroupComposition composition)
    {
        var scoreText = composition.TotalScore.ToString("0.0", CultureInfo.InvariantCulture);
        var groups = composition.Groups
            .Select((group, index) => GroupCardViewModel.FromGroup(group, index + 1))
            .ToList();

        return new GroupCompositionCardViewModel(scoreText, groups);
    }
}
