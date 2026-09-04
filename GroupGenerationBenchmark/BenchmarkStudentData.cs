using Logic.Models;

namespace GroupGenerationBenchmark;

internal static class BenchmarkStudentData
{
    public static StudentList CreateStudents30() => StudentList.Create(
    [
        S("482910", "Emma Smith", ["847519", "103948"], ["847519", "103948", "294857"], ["294857"]),
        S("847519", "Liam Johnson", ["294857"], ["482910", "103948", "294857"]),
        S("103948", "Olivia Williams", ["482910", "758392", "920481"], ["482910", "847519", "294857"], ["294857", "384729"]),
        S("294857", "Noah Brown", [], ["482910", "847519", "103948"]),
        S("758392", "Ava Jones", ["103948", "847519"], ["920481", "384729", "619284"], ["920481"]),
        S("920481", "Ethan Miller", ["384729"], ["758392", "384729", "619284"]),
        S("384729", "Sophia Davis", ["294857", "482910", "103948"], ["758392", "920481", "619284"]),
        S("619284", "Mason Wilson", ["582910", "748392"], ["758392", "920481", "384729"]),
        S("582910", "Isabella Moore", [], ["748392", "392014", "857492"]),
        S("748392", "James Taylor", ["619284", "847519"], ["582910", "392014", "857492"]),
        S("392014", "Charlotte Anderson", ["920481", "103948"], ["582910", "748392", "857492"]),
        S("857492", "Benjamin Thomas", ["392014"], ["582910", "748392", "392014"]),
        S("192834", "Amelia Jackson", ["857492", "482910", "294857"], ["683920", "502938", "819203"]),
        S("683920", "Lucas White", [], ["192834", "502938", "819203"]),
        S("502938", "Mia Harris", ["192834", "683920"], ["192834", "683920", "819203"]),
        S("819203", "Henry Martin", ["502938", "384729"], ["192834", "683920", "502938"]),
        S("203948", "Harper Thompson", ["819203", "758392", "103948"], ["495832", "918273", "627384"]),
        S("495832", "Evelyn Garcia", ["203948", "482910"], ["203948", "918273", "627384"]),
        S("918273", "Alexander Martinez", [], ["203948", "495832", "627384"]),
        S("627384", "Abigail Robinson", ["495832", "918273", "857492"], ["203948", "495832", "918273"]),
        S("142536", "Daniel Clark", ["627384"], ["738495", "364758", "891023", "514236"]),
        S("738495", "Emily Rodriguez", ["142536", "103948", "847519"], ["142536", "364758", "891023", "514236"]),
        S("364758", "Matthew Lewis", [], ["142536", "738495", "891023", "514236"]),
        S("891023", "Elizabeth Lee", ["738495", "364758"], ["142536", "738495", "364758", "514236"]),
        S("514236", "Aiden Walker", ["891023", "627384"], ["142536", "738495", "364758", "891023"]),
        S("983726", "Sofia Hall", ["514236", "142536", "294857"], ["273849", "654789", "412356", "893412"]),
        S("273849", "Jackson Allen", ["983726", "482910"], ["983726", "654789", "412356", "893412"]),
        S("654789", "Aurelia Young", [], ["983726", "273849", "412356", "893412"]),
        S("412356", "Logan King", ["654789", "273849", "819203"], ["983726", "273849", "654789", "893412"]),
        S("893412", "Chloe Wright", ["412356", "103948"], ["983726", "273849", "654789", "412356"]),
    ]);

    /// <summary>
    /// Crafted 50-student set: sparse 0–3 positive wishes in small clusters,
    /// a few mutual pairs, and ~7 students with 1–2 negative wishes.
    /// </summary>
    public static StudentList CreateStudents50()
    {
        // IDs 100001–100050. Clusters of ~5 with local affinity; some mutuals; sparse negatives.
        return StudentList.Create(
        [
            // Cluster A (mutual 01↔02, chain 03→01)
            S("100001", "Nora Blake", ["100002", "100003"]),
            S("100002", "Owen Clark", ["100001", "100004"]),
            S("100003", "Piper Dunn", ["100001"]),
            S("100004", "Quinn Ellis", ["100002", "100005"]),
            S("100005", "Riley Frost", []),

            // Cluster B
            S("100006", "Sam Greene", ["100007", "100008"]),
            S("100007", "Tara Hayes", ["100006"]), // mutual with 06
            S("100008", "Uma Ingram", ["100009"]),
            S("100009", "Vince Jones", ["100008", "100010"]), // mutual with 08
            S("100010", "Wendy Kane", ["100006"], negative: ["100015"]),

            // Cluster C
            S("100011", "Xander Lane", ["100012", "100013", "100014"]),
            S("100012", "Yara Moss", ["100011"]), // mutual with 11
            S("100013", "Zane Nash", []),
            S("100014", "Aria Ortiz", ["100013", "100015"]),
            S("100015", "Blake Price", ["100014"], negative: ["100010", "100020"]),

            // Cluster D
            S("100016", "Cora Quinn", ["100017"]),
            S("100017", "Drew Reed", ["100016", "100018"]), // mutual with 16
            S("100018", "Elle Scott", ["100019"]),
            S("100019", "Finn Turner", []),
            S("100020", "Gina Underwood", ["100016", "100021"], negative: ["100015"]),

            // Cluster E
            S("100021", "Hugo Vale", ["100022", "100023"]),
            S("100022", "Iris Webb", ["100021"]), // mutual with 21
            S("100023", "Joel Xu", ["100024"]),
            S("100024", "Kate Young", ["100023", "100025"]), // mutual with 23
            S("100025", "Leo Zimmer", []),

            // Cluster F
            S("100026", "Maya Adams", ["100027", "100028", "100029"]),
            S("100027", "Nate Brooks", ["100026"]), // mutual with 26
            S("100028", "Olive Chen", []),
            S("100029", "Paul Diaz", ["100030"]),
            S("100030", "Ruby Evans", ["100029", "100026"], negative: ["100035"]),

            // Cluster G
            S("100031", "Seth Ford", ["100032"]),
            S("100032", "Tessa Grant", ["100031", "100033"]), // mutual with 31
            S("100033", "Uri Holst", ["100034"]),
            S("100034", "Vera Ives", []),
            S("100035", "Will Jung", ["100031"], negative: ["100030", "100040"]),

            // Cluster H
            S("100036", "Xena Klein", ["100037", "100038"]),
            S("100037", "Yuri Lang", ["100036"]), // mutual with 36
            S("100038", "Zoe Mills", ["100039"]),
            S("100039", "Adam Novak", ["100038", "100040"]),
            S("100040", "Bella Owens", [], negative: ["100035"]),

            // Cluster I (cross-links into H and J)
            S("100041", "Carl Perez", ["100042", "100036"]),
            S("100042", "Dana Roth", ["100041", "100043"]), // mutual with 41
            S("100043", "Evan Shaw", []),
            S("100044", "Faye Tao", ["100045"]),
            S("100045", "Gabe Ulrich", ["100044", "100046"], negative: ["100050"]),

            // Cluster J
            S("100046", "Holly Vance", ["100047"]),
            S("100047", "Ian Walsh", ["100046", "100048"]), // mutual with 46
            S("100048", "Julia York", ["100049"]),
            S("100049", "Kyle Zane", []),
            S("100050", "Lila Avery", ["100048", "100041"], negative: ["100045"]),
        ]);
    }

    private static Student S(
        string number,
        string name,
        string[] positive,
        string[]? previous = null,
        string[]? negative = null)
        => Student.Create(
            number,
            positive.Select(StudentNumber.Create).ToList(),
            name,
            previous?.Select(StudentNumber.Create).ToList(),
            negative?.Select(StudentNumber.Create).ToList());
}
