using Logic.Models;

namespace Logic.Grouping.Generation;

public class SlidingWindowStrategy : IGroupCompositionProducer
{
    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}