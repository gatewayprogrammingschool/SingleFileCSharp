
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

    using var reader = new StreamReader(System.IO.File.OpenRead(filename));

    var fi = new FileInfo(reader);
    var basename = fi.Name[0..(fi.Name.LastIndexOf('.'))];
    var pageNumber = 0;
    var currentName = $"{basename}-{++pageNumber}.md";

    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();

        if (line.StartsWith("# "))
        {
            WriteFile(sb.ToString(), currentName);
            sb.Clear();
            currentName = $"{basename}-{++pageNumber}.md";
        }

        sb.AppendLine(line);
    }

    WriteFile(sb.ToString(), currentName);
}

static void WriteFile(string text, string fileName)
{
    if (sb.Length > 0)
    {
        FileInfo.WriteAllText(fileName, text);

        Console.WriteLine($"Saved: {fileName}");
    }
}

Console.WriteLine("Done");
