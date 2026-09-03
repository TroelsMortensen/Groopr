using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Logic.Models;

namespace Logic.Import;

public sealed class StudentCsvImporter
{
    private static readonly string[] RequiredHeaders = ["StudentNumber", "Name", "PositiveWishes"];

    private static readonly string[] AllowedHeaders =
        ["StudentNumber", "Name", "PositiveWishes", "PreviousGroupMembers", "NegativeWishes"];

    public StudentList Import(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.Trim(),
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader())
        {
            throw new StudentCsvImportException("The CSV file is empty.");
        }

        ValidateHeaders(csv.HeaderRecord);
        var columnIndexes = BuildColumnIndexMap(csv.HeaderRecord!);

        var students = new List<Student>();
        var rowNumber = 1;

        while (csv.Read())
        {
            rowNumber++;
            students.Add(ParseRow(csv, columnIndexes, rowNumber));
        }

        if (students.Count == 0)
        {
            throw new StudentCsvImportException("The CSV file contains no student rows.");
        }

        try
        {
            return StudentList.Create(students);
        }
        catch (Exception ex) when (ex is not StudentCsvImportException)
        {
            throw new StudentCsvImportException(ex.Message, innerException: ex);
        }
    }

    private static void ValidateHeaders(string[]? headers)
    {
        if (headers is null || headers.Length == 0)
        {
            throw new StudentCsvImportException("The CSV file is missing a header row.");
        }

        var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);
        var allowedSet = new HashSet<string>(AllowedHeaders, StringComparer.OrdinalIgnoreCase);

        foreach (var required in RequiredHeaders)
        {
            if (!headerSet.Contains(required))
            {
                throw new StudentCsvImportException($"Missing required column '{required}'.");
            }
        }

        foreach (var header in headers)
        {
            if (!allowedSet.Contains(header.Trim()))
            {
                throw new StudentCsvImportException(
                    $"Unexpected column '{header.Trim()}'. Allowed columns: {string.Join(", ", AllowedHeaders)}.");
            }
        }
    }

    private static Dictionary<string, int> BuildColumnIndexMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            map[headers[i].Trim()] = i;
        }

        return map;
    }

    private static Student ParseRow(CsvReader csv, IReadOnlyDictionary<string, int> columnIndexes, int rowNumber)
    {
        try
        {
            var number = GetField(csv, columnIndexes, "StudentNumber");
            if (string.IsNullOrWhiteSpace(number))
            {
                throw new StudentCsvImportException("Student number is required.", rowNumber);
            }

            var nameField = GetField(csv, columnIndexes, "Name");
            var name = string.IsNullOrWhiteSpace(nameField) ? null : nameField.Trim();
            var wishes = ParseStudentNumbers(GetField(csv, columnIndexes, "PositiveWishes"));
            var previousGroupMembers = ParseStudentNumbers(
                GetOptionalField(csv, columnIndexes, "PreviousGroupMembers"));
            var negativeWishes = ParseStudentNumbers(
                GetOptionalField(csv, columnIndexes, "NegativeWishes"));

            return Student.Create(
                number.Trim(),
                wishes.Select(StudentNumber.Create).ToList(),
                name,
                previousGroupMembers.Select(StudentNumber.Create).ToList(),
                negativeWishes.Select(StudentNumber.Create).ToList());
        }
        catch (StudentCsvImportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new StudentCsvImportException(ex.Message, rowNumber, ex);
        }
    }

    private static string? GetField(CsvReader csv, IReadOnlyDictionary<string, int> columnIndexes, string columnName)
    {
        if (!columnIndexes.TryGetValue(columnName, out var index))
        {
            throw new StudentCsvImportException($"Missing required column '{columnName}'.");
        }

        return csv.GetField(index);
    }

    private static string? GetOptionalField(
        CsvReader csv,
        IReadOnlyDictionary<string, int> columnIndexes,
        string columnName) =>
        columnIndexes.TryGetValue(columnName, out var index) ? csv.GetField(index) : null;

    private static List<string> ParseStudentNumbers(string? input) =>
        (input ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static s => s.Length > 0)
            .ToList();
}
