using Logic.Models;

namespace Logic.Grouping.Generation;

public interface IGroupCompositionProducer
{
    public IEnumerable<GroupComposition> GenerateStream(CancellationToken cancellationToken);
}