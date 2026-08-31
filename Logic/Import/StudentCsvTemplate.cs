namespace Logic.Import;

public static class StudentCsvTemplate
{
    public const string Header = "StudentNumber,Name,PositiveWishes";

    public static string GetContent() =>
        string.Join(Environment.NewLine,
        [
            Header,
            "123456,John Doe,\"654321, 789654\"",
        ]);
}
