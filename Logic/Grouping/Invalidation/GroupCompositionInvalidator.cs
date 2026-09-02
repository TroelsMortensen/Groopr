using Logic.Grouping.Invalidation.InvalidationStrategies;
using Logic.Models;

namespace Logic.Grouping.Invalidation;

public class GroupCompositionInvalidator(List<IInvalidator> invalidators)
{
    public bool ShouldReject(GroupComposition groupComposition) =>
        invalidators.Any(i => i.ShouldReject(groupComposition));
}