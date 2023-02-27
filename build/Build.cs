using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;

[ShutdownDotNetAfterServerBuild]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
class Build : NukeBuild
{
    const string NuGetSource = "https://api.nuget.org/v3/index.json";
    
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("NuGet API Key", Name = "NUGET_API_KEY_AUTOCONF")] 
    [CanBeNull] readonly string NuGetApiKey;
    
    [GitVersion] [CanBeNull] readonly GitVersion GitVersion;
    [Solution] [CanBeNull] readonly Solution Solution;
    
    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath OutputDirectory => RootDirectory / "output";
    static AbsolutePath PackageOutputDirectory => OutputDirectory / "packages";
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory
                .GlobDirectories("**/bin", "**/obj")
                .ForEach(DeleteDirectory);

            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(settings => settings.SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(settings =>
                settings.SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetAssemblyVersion(GitVersion?.AssemblySemVer)
                    .SetFileVersion(GitVersion?.AssemblySemFileVer)
                    .SetInformationalVersion(GitVersion?.InformationalVersion)
                    .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(settings =>
                settings.SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableCollectCoverage()
                    .EnableBlameHang()
                    .SetBlameHangTimeout("60s")
                    .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
                    .SetDataCollector("XPlat Code Coverage")
                    .SetFilter("FullyQualifiedName!~IntegrationTests")
                    .EnableNoBuild()
                    .EnableNoRestore());
        });
    
    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            var packableProjects = Solution?
                .AllProjects
                .Where(project => project.GetProperty<bool>("IsPackable")) ?? Enumerable.Empty<Project>();

            foreach (var project in packableProjects)
            {
                Serilog.Log.Information("Packaging project '{ProjectName}'...", project.Name);
                
                DotNetTasks.DotNetPack(settings => settings
                    .SetProject(project)
                    .SetOutputDirectory(PackageOutputDirectory)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetVersion(GitVersion?.NuGetVersionV2)
                    .SetAssemblyVersion(GitVersion?.AssemblySemVer)
                    .SetFileVersion(GitVersion?.AssemblySemFileVer)
                    .SetInformationalVersion(GitVersion?.InformationalVersion));
            }
        });
    
    Target Publish => _ => _
        .DependsOn(Pack)
        .Requires(() => !string.IsNullOrWhiteSpace(NuGetApiKey))
        .Executes(() =>
        {
            var packageFiles = PackageOutputDirectory.GlobFiles("*.nupkg");

            foreach (var packageFile in packageFiles)
            {
                Serilog.Log.Information("Pushing '{PackageName}'...", packageFile.Name);
                
                DotNetTasks.DotNetNuGetPush(settings => settings
                    .SetApiKey(NuGetApiKey)
                    .SetSymbolApiKey(NuGetApiKey)
                    .SetTargetPath(packageFile)
                    .SetSource(NuGetSource)
                    .SetSymbolSource(NuGetSource));
            }
        });
}
