namespace Logic.Import;

public static class StudentCsvTemplate
{
    public const string Header = "StudentNumber,Name,PositiveWishes,PreviousGroupMembers,NegativeWishes";

    public static string GetContent() =>
        string.Join(Environment.NewLine,
        [
            Header,
            "100001,Alice,\"100002, 100003\",\"100002, 100003\",",
            "100002,Bob,100001,100001,100003",
            "100003,,,,",
        ]);
}
