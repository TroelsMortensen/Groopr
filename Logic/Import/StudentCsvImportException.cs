namespace Logic.Import;

public sealed class StudentCsvImportException : Exception
{
    public int? RowNumber { get; }

    public StudentCsvImportException(string message, int? rowNumber = null, Exception? innerException = null)
        : base(FormatMessage(message, rowNumber), innerException)
    {
        RowNumber = rowNumber;
    }

    private static string FormatMessage(string message, int? rowNumber) =>
        rowNumber is null ? message : $"Error on row {rowNumber}: {message}";
}
