/*****************************
 * Helpers
 *****************************/

private static ExtensionHelper extensionHelper;
extensionHelper = new ExtensionHelper(Task, () => RunTarget(Argument("target", "Default")));
public static CakeTaskBuilder Then(this CakeTaskBuilder cakeTaskBuilder, string name)
    => extensionHelper
        .TaskCreate(name)
        .IsDependentOn(cakeTaskBuilder);


public static CakeReport Run(this CakeTaskBuilder cakeTaskBuilder)
    => extensionHelper.Run();

public static CakeTaskBuilder Default(this CakeTaskBuilder cakeTaskBuilder)
{
    extensionHelper
        .TaskCreate("Default")
        .IsDependentOn(cakeTaskBuilder);
    return cakeTaskBuilder;
}

if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
{
    TaskSetup(context=> System.Console.WriteLine($"::group::{context.Task.Name.Quote()}"));
    TaskTeardown(context=>System.Console.WriteLine("::endgroup::"));
}