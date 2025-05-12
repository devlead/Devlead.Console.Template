// Install .NET Core Global tools.
#tool "dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=6.3.0"

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
        var buildDate = DateTime.UtcNow;
        var runNumber = gh.IsRunningOnGitHubActions
                            ? gh.Environment.Workflow.RunNumber
                            : (short)((buildDate - buildDate.Date).TotalSeconds/3);
        var version = FormattableString
                        .Invariant($"{buildDate:yyyy.M.d}.{runNumber}");

        var branchName = assertedVersions.BranchName;
        var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("main", branchName);
        var isDevelopmentBranch = StringComparer.OrdinalIgnoreCase.Equals("development", branchName);

        context.Information("Building version {0} (Branch: {1}, IsMain: {2}, IsDevelopment: {3})",
            version,
            branchName,
            isMainBranch,
            isDevelopmentBranch);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        return new BuildData(
            version,
            isMainBranch,
            isDevelopmentBranch,
            "src",
            "src/Devlead.Console.Template/Devlead.Console.Template.csproj",
            artifactsPath,
            artifactsPath.Combine(version),
            (data, msbuildsetting) => new DotNetMSBuildSettings
                                                                {
                                                                    ArgumentCustomization = args => args
                                                                                                        .AppendQuoted("/property:TargetFrameworks=\\\"net8.0;net9.0\\\"")
                                                                                                        .Append(msbuildsetting.Targets.Contains("Pack") ? string.Empty : "-restore"),
                                                                }
                                                                    .SetConfiguration("Release")
                                                                    .SetVersion(data.Version)
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
                                                                    .WithProperty("EmbedUntrackedSources", "true")
                                                                    .WithProperty("PackageOutputPath", data.NuGetOutputPath.FullPath)
                                                                    .WithProperty("BaseOutputPath", data.BinaryOutputPath.FullPath + "/")
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
    .Does<BuildData>(
        static (context, data) => context.DotNetMSBuild(
            data.ProjectRoot.FullPath,
            data
                .MSBuildSettings
        )
    )
.Then("Restore")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            data.ProjectRoot.FullPath,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Build")
    .Does<BuildData>(
        static (context, data) => context.DotNetBuild(
            data.ProjectRoot.FullPath,
            new DotNetBuildSettings {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Test")
    .Does<BuildData>(
        static (context, data) => context.DotNetTest(
            data.ProjectRoot.FullPath,
            new DotNetTestSettings {
                NoBuild = true,
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Integration-Tests")
    .Default()
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.BinaryOutputPath.FullPath + "/**/devleadconsole.dll").ToArray()
                is { Length: > 0 } files
                    ? files
                    : throw new CakeException("No integration tests found"),
        static (data, item, context)
            => context.DotNetTool(
                item.FullPath,
                new DotNetToolSettings {
                    ArgumentCustomization = args => {
                        context.Information("Testing {0}...", item);
                        
                        return args 
                                .Append("console")
                                .Append("mandatory")
                                .Append("--command-option-flag")
                                .AppendSwitchQuoted("--command-option-value", "=", data.Version);
                    },
                    HandleExitCode = exitCode => {
                        if (exitCode == 0)
                        {
                            context.Information("Successfully tested {0}", item);
                            return true;
                        }

                        throw new CakeException($"Failed testing {item} ({exitCode})");
                    }
                }
            )
    )
    .DeferOnError()
.Then("DPI")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
                "tool",
                new DotNetToolSettings {
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
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetMSBuild(
            data.TemplateProject.FullPath,
            data
                .MSBuildSettings
                .WithTarget("Pack")
                .WithProperty("_IsPacking", "true")
                .WithProperty("NoBuild", "true")
        )
    )
.Then("Upload-Artifacts")
    .WithCriteria(BuildSystem.IsRunningOnGitHubActions, nameof(BuildSystem.IsRunningOnGitHubActions))
    .Does<BuildData>(
        static (context, data) => context
            .GitHubActions()
            .Commands
            .UploadArtifact(data.ArtifactsPath, "artifacts")
    )
.Then("Push-GitHub-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushGitHubPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
            new DotNetNuGetPushSettings
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
            => context.DotNetNuGetPush(
                item.FullPath,
                new DotNetNuGetPushSettings
                {
                    Source = data.NuGetSource,
                    ApiKey = data.NuGetApiKey
                }
        )
    )
.Then("Create-GitHub-Release")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .Does<BuildData>(
        static (context, data) => context
            .Command(
                new CommandSettings {
                    ToolName = "GitHub CLI",
                    ToolExecutableNames = new []{ "gh.exe", "gh" },
                    EnvironmentVariables = { { "GH_TOKEN", data.GitHubNuGetApiKey } }
                },
                new ProcessArgumentBuilder()
                    .Append("release")
                    .Append("create")
                    .Append(data.Version)
                    .AppendSwitchQuoted("--title", data.Version)
                    .Append("--generate-notes")
                    .Append(string.Join(
                        ' ',
                        context
                            .GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg")
                            .Select(path => path.FullPath.Quote())
                        ))

            )
    )
.Then("GitHub-Actions")
.Run();