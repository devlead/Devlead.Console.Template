// Install .NET Core Global tools.
#tool "dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=5.1.2"

#load "build/records.cake"
#load "build/helpers.cake"

/*****************************
 * Setup
 *****************************/
Setup(
    static context => {

        var assertedVersions = context.GitVersion(new GitVersionSettings
            {
                OutputType = GitVersionOutput.Json
            });

        var gh = context.GitHubActions();
        var version = assertedVersions.LegacySemVerPadded;
        var branchName = assertedVersions.BranchName;
        var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("main", branchName);

        context.Information("Building version {0} (Branch: {1}, IsMain: {2})",
            version,
            branchName,
            isMainBranch);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        return new BuildData(
            version,
            isMainBranch,
            "src",
            "src/Devlead.Console.Template/Devlead.Console.Template.csproj",
            new DotNetCoreMSBuildSettings()
                .SetConfiguration("Release")
                .SetVersion(version)
                .WithProperty("Copyright", $"Mattias Karlsson © {DateTime.UtcNow.Year}")
                .WithProperty("Authors", "devlead")
                .WithProperty("Company", "devlead")
                .WithProperty("PackageLicenseExpression", "MIT")
                .WithProperty("PackageTags", "Console;Template")
                .WithProperty("PackageDescription", "Mattias Karlsson's optinionated alternative to dotnet new console")
                .WithProperty("PackageIconUrl", "https://cdn.jsdelivr.net/gh/devlead/Devlead.Console.Template/src/devlead.png")
                .WithProperty("PackageIcon", "devlead.png")
                .WithProperty("PackageProjectUrl", "https://github.com/devlead/Devlead.Console.Template")
                .WithProperty("RepositoryUrl", "https://github.com/devlead/Devlead.Console.Template.git")
                .WithProperty("RepositoryType", "git")
                .WithProperty("ContinuousIntegrationBuild", gh.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version)
            );
    }
);

/*****************************
 * Tasks
 *****************************/
Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    )
.Then("Restore")
    .Does<BuildData>(
        static (context, data) => context.DotNetCoreRestore(
            data.ProjectRoot.FullPath,
            new DotNetCoreRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("DPI")
    .Does<BuildData>(
        static (context, data) => context.DotNetCoreTool(
                "tool",
                new DotNetCoreToolSettings {
                    ArgumentCustomization = args => args
                                                        .Append("run")
                                                        .Append("dpi")
                                                        .Append("nuget")
                                                        .Append("--silent")
                                                        .AppendSwitchQuoted("--output", "table")
                                                        .Append(
                                                            (
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_SharedKey"))
                                                                &&
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_WorkspaceId"))
                                                            )
                                                                ? "report"
                                                                : "analyze"
                                                            )
                                                        .AppendSwitchQuoted("--buildversion", data.Version)
                }
            )
    )
.Then("Build")
    .Default()
    .Does<BuildData>(
        static (context, data) => context.DotNetCoreBuild(
            data.ProjectRoot.FullPath,
            new DotNetCoreBuildSettings {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetCorePack(
            data.TemplateProject.FullPath,
            new DotNetCorePackSettings {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Push-GitHub-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushGitHubPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetCoreNuGetPush(
                item.FullPath,
            new DotNetCoreNuGetPushSettings
            {
                Source = data.GitHubNuGetSource,
                ApiKey = data.GitHubNuGetApiKey
            }
        )
    )
.Then("Push-NuGet-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetCoreNuGetPush(
                item.FullPath,
                new DotNetCoreNuGetPushSettings
                {
                    Source = data.NuGetSource,
                    ApiKey = data.NuGetApiKey
                }
        )
    )
.Then("GitHub-Actions")
.Run();