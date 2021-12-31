
var wd = ".";

if (args.Length > 0)
{
    wd = args[0];
}

Console.WriteLine($"args.Length: {args.Length}");
Console.WriteLine($"wd: {wd}");

var mdFiles = Directory.GetFiles(wd, "*.md");

Console.WriteLine(string.Join(Environment.NewLine, mdFiles));

Console.WriteLine("Done");
