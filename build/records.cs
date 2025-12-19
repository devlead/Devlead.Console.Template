/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    bool IsMainBranch,
    bool IsDevelopmentBranch,
    DirectoryPath ProjectRoot,
    FilePath TemplateProject,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath,
    Func<BuildData, DotNetMSBuildSettings, DotNetMSBuildSettings> MSBuildSettingsCustomization
    )
{
    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");
    public DirectoryPath BinaryOutputPath { get; } = OutputPath.Combine("bin");

    public string? GitHubNuGetSource { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_SOURCE");
    public string? GitHubNuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    public bool ShouldPushGitHubPackages() => (IsMainBranch || IsDevelopmentBranch) &&
                                                !string.IsNullOrWhiteSpace(GitHubNuGetSource)
                                                && !string.IsNullOrWhiteSpace(GitHubNuGetApiKey);

    public string? NuGetSource { get; } = System.Environment.GetEnvironmentVariable("NUGET_SOURCE");
    public string? NuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
    public bool ShouldPushNuGetPackages() =>    IsMainBranch &&
                                                !string.IsNullOrWhiteSpace(NuGetSource) &&
                                                !string.IsNullOrWhiteSpace(NuGetApiKey);

    public ICollection<DirectoryPath> DirectoryPathsToClean { get; } = new []{
        ArtifactsPath,
        OutputPath
    };

    private DotNetMSBuildSettings msBuildSettings;
    public DotNetMSBuildSettings MSBuildSettings => msBuildSettings ??= MSBuildSettingsCustomization(this, new DotNetMSBuildSettings());
}

internal record ExtensionHelper(Func<string, CakeTaskBuilder> TaskCreate, Func<CakeReport> Run);
