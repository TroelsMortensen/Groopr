using System.Collections.Generic;
using System.Linq;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupCompositionGeneration;

public partial class GroupCardViewModel
{
    public string Title { get; }

    public IReadOnlyList<GroupMemberViewModel> Members { get; }

    private GroupCardViewModel(string title, IReadOnlyList<GroupMemberViewModel> members)
    {
        Title = title;
        Members = members;
    }

    public static GroupCardViewModel FromGroup(Group group, int groupNumber) =>
        new(
            $"Group {groupNumber}",
            group.Members.Select(GroupMemberViewModel.FromStudent).ToList());
}
