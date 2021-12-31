// ReSharper disable NotAccessedField.Local
// ReSharper disable AnnotateNotNullTypeMember
// ReSharper disable UnusedMember.Local
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable EmptyNamespace

namespace SingleFileCSharp;

using static StringComparison;

[ CheckBuildProjectConfigurations, ShutdownDotNetAfterServerBuild, ]
partial class Build : NukeBuild
{
    [ Solution ]
    protected Solution? Solution { get; }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(static () => FileSystemTasks.EnsureCleanDirectory(ArtifactsDirectory));

    Target Restore => _ =>
    {
        IReadOnlyCollection<Output> Actions()
            => DotNetTasks.DotNetRestore(s =>
                {
                    s = s.SetProjectFile(Solution)
                        .SetProcessWorkingDirectory(Solution!.Directory);

                    if (File.Exists(Solution!.Directory / "nuget.config"))
                    {
                        s = s.SetConfigFile(Solution.Directory / "nuget.config");
                    }

                    return s;
                }
            );

        return _
            .Executes(Actions);
    };

    Target Compile => _ => _
        .DependsOn(Expand)
        .DependsOn(Restore)
        .Executes(() =>
            DotNetTasks.DotNetBuild(s =>
                {
                    s = s.SetProjectFile(Solution)
                        .SetConfiguration(_configuration)
                        .SetProcessWorkingDirectory(Solution!.Directory)
                        .EnableNoRestore();

                    if (_gitVersion is not null)
                    {
                        s = s.SetAssemblyVersion(_gitVersion?.AssemblySemVer)
                            .SetFileVersion(_gitVersion?.AssemblySemFileVer)
                            .SetInformationalVersion(_gitVersion?.InformationalVersion);
                    }

                    return s;
                }
            )
        );

    Target Run => _ => _
        .DependsOn(Compile)
        .Executes(static () =>
            {
                IReadOnlyCollection<AbsolutePath> files = RootDirectory
                    .GlobFiles("**/bin/**/*.exe");

                foreach (AbsolutePath path in files)
                {
                    if (path.Contains("build.exe"))
                    {
                        continue;
                    }

                    var index = Environment.CommandLine.IndexOf(" -- ", Ordinal) + " -- ".Length;
                    var args = "";
                    if (index > -1)
                    {
                        args = Environment.CommandLine[index..];
                    }

                    ProcessStartInfo info = new()
                    {
                        FileName = path,
                        Arguments = args,
                        WorkingDirectory = Path.GetDirectoryName(path),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    Process? process = Process.Start(info);

                    process?.WaitForExit();

                    Log.Information(process?.StandardOutput.ReadToEnd());

                    if (process?.ExitCode != 0)
                    {
                        Log.Error(process?.StandardError.ReadToEnd());
                    }
                }
            }
        );

    Target Push => _ => _
        .DependsOn(Expand)
        .Executes(() =>
            {
                ProcessStartInfo info = new()
                {
                    FileName = "git",
                    Arguments =
                        $"push https://sharpninja:{GithubToken}@github.com/sharpninja/CSharpExploration HEAD:main",
                    WorkingDirectory = RootDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                Process? process = Process.Start(info);

                process?.WaitForExit();

                Log.Information(process?.StandardOutput.ReadToEnd());

                if ((process?.ExitCode ?? -1) != 0)
                {
                    Console.Error.WriteLine(process?.StandardError.ReadToEnd());
                }
            }
        );

    Target Expand => _ => _
        .Executes(() =>
            {
                const RegexOptions REGEX_OPTIONS =
                    RegexOptions.IgnoreCase |
                    RegexOptions.Compiled |
                    RegexOptions.Singleline;

                IOrderedEnumerable<AbsolutePath> files = RootDirectory
                    .GlobFiles("**/*.cs")
                    .OrderBy(static f => f.ToString());

                // Pattern can be either regular RegEx
                // or plain string.  Both are executed
                // case-insensitive.
                List<object> skipPatterns = new()
                {
                    new Regex(@"\b(build|obj)\b", REGEX_OPTIONS),
                };

                var expandedCount = 0;
                var expanded = false;

                foreach (string file in files)
                {
                    string? dirPath = Path.GetDirectoryName(file);

                    if (dirPath is null or "")
                    {
                        Log.Warning($"Could not get directory name for [{file}]");

                        continue;
                    }

                    var toBreak = false;

                    foreach (object pattern in skipPatterns)
                    {
                        switch (pattern)
                        {
                            case Regex r:
                                if (r.IsMatch(dirPath!))
                                {
                                    toBreak = true;
                                }
                                else
                                {
                                    Log.Information($"[Expand] [{r}] does not match [{dirPath}]");
                                }

                                break;

                            case string s:
                                //Debugger.Launch();
                                if (dirPath?.Contains(s,
                                        InvariantCultureIgnoreCase
                                    ) ==
                                    true)
                                {
                                    toBreak = true;
                                }
                                else
                                {
                                    Log.Information($"[Expand] [{s}] does not match [{dirPath}]");
                                }

                                break;

                            default:
                                throw new InvalidCastException(
                                    $"[Expand] Pattern is wrong type: {pattern.GetType().Name}"
                                );
                        }

                        if (toBreak)
                        {
                            break;
                        }
                    }

                    if (toBreak)
                    {
                        continue;
                    }

                    bool didExpand = ProcessFile(file);

                    if (didExpand)
                    {
                        expandedCount++;
                    }

                    expanded |= didExpand;
                }

                if (!expanded)
                {
                    return;
                }

                Log.Information($"[Expand] Expanded {expandedCount} files.");

                ProcessStartInfo info = new()
                {
                    FileName = "git",
                    Arguments = $"commit -a -m \"Expanded {expandedCount} files.\"",
                    WorkingDirectory = NukeBuild.RootDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                Process? process = Process.Start(info);

                process?.WaitForExit();

                Log.Information(process?.StandardOutput.ReadToEnd());

                if ((process?.ExitCode ?? -1) != 0)
                {
                    Console.Error.WriteLine(process?.StandardError.ReadToEnd());
                }
            }
        );

    public Build()
        => _instance = this;

    public static int Main()
        => Execute<Build>(static x => x.Run);

    [ Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)") ]
    protected readonly Configuration _configuration = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;

    [ GitRepository ] protected readonly GitRepository? _gitRepository;
    [ GitVersion ] protected readonly GitVersion? _gitVersion;
    // ReSharper disable once InconsistentNaming
    string? _githubToken;

}
