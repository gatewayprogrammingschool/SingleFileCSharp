/// Project:
///   _Sdk: Microsoft.NET.Sdk
///   PropertyGroup:
///   - TargetFramework: net6.0
///     OutputType: Exe
///     ImplicitUsings: True
///     Nullable: Enable
///     DefaultNamespace: MarkdownSplitter

using System.Text;

var wd = ".";

if (args.Length > 0)
{
    wd = args[0];
}

Console.WriteLine($"args.Length: {args.Length}");
Console.WriteLine($"wd: {wd}");

var mdFiles = Directory.GetFiles(wd, "*.md");

Console.WriteLine(string.Join(Environment.NewLine, mdFiles));

foreach (var filename in mdFiles)
{
    var sb = new StringBuilder();

    var reader = new StreamReader(System.IO.File.OpenRead(filename));

    var fi = new FileInfo(filename);
    var basename = fi.Name[0..(fi.Name.LastIndexOf('.'))];
    var baseDirectory = Path.Combine(wd, basename);
    var pageNumber = 0;
    var currentName = Path.Combine(baseDirectory, $"{basename}-{pageNumber++}.md");

    if (!Directory.Exists(baseDirectory))
    {
        Directory.CreateDirectory(baseDirectory);
    }

    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();

        if (line.StartsWith("# "))
        {
            WriteFile(sb.ToString(), currentName);
            sb.Clear();
            currentName = Path.Combine(baseDirectory, $"{basename}-{pageNumber++}.md");
        }

        sb.AppendLine(line);
    }

    reader.Close();
    reader.Dispose();

    WriteFile(sb.ToString(), currentName);
}

static void WriteFile(string text, string fileName)
{
    if (text.Length > 0)
    {
        File.WriteAllText(fileName, text);

        Console.WriteLine($"Saved: {fileName}");
    }
}

Console.WriteLine("Done");
