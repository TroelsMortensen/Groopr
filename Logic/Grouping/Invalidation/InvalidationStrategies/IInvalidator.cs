using Logic.Models;

namespace Logic.Grouping.Invalidation.InvalidationStrategies;

public interface IInvalidator
{
    
    bool ShouldReject(GroupComposition groupComposition);
}