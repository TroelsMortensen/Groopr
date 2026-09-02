using Logic.Models;

namespace Logic.Grouping.Invalidation.InvalidationStrategies;

public class MaxNumberOfStudentsFromPreviousGroup(int numberOfStudents) : IInvalidator
{
    public bool ShouldReject(GroupComposition groupComposition) =>
        groupComposition.Groups.Any(group =>
        {
            // Keep the hashset for O(1) lookups per group
            var memberNumbers = group.Members.Select(m => m.Number).ToHashSet();

            bool any = group.Members.Any(student =>
                student.PreviousGroupMembers.Count(pm => memberNumbers.Contains(pm.Value)) > numberOfStudents);
            if (any)
            {
                ;
            }
            return any;
        });
}