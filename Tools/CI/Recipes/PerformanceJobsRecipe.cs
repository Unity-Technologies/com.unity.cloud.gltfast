using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.UnityEditor;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Unity.Abstractions.Editors;

namespace Gltfast.Cookbook.Recipes;

/// <summary>
/// A recipe to create performance jobs.
/// </summary>
class PerformanceJobsRecipe : RecipeBase
{
    const string k_EditorPath = "Editor";
    const string k_ProjectPath = "Projects/glTFast-Test";
    const string k_ArtifactsPath = "test-results~";

    IEnumerable<string> m_SupportedEditorVersions;

    public PerformanceJobsRecipe(WrenchSettings settings)
    {
        Name = "Performance";
        Description = "Contains jobs that run performance tests.";
        m_SupportedEditorVersions = settings.Packages["com.unity.cloud.gltfast"].SupportedEditorVersions;
    }

    protected override ISet<Job> LoadJobs()
    {
        var jobs = new HashSet<IJobBuilder>();
        foreach (var editorVersion in m_SupportedEditorVersions)
        {
            jobs.Add(CreateJob(editorVersion));
        }
        jobs.Add(CreateAllJob(jobs));
        return jobs.SelectJobs();
    }

    static IJobBuilder CreateJob(string editorVersion)
    {
        var commands = new List<Command>();
        commands.Add(UnityEditorCommand.Download(new Editor(editorVersion, editorVersion), "unity-downloader-cli", k_EditorPath).ToRetryCommand(3, 10));
        commands.Add(UnityEditorCommand.Execute($"{k_EditorPath}\\Unity.exe", EnablePerformanceTests));
        if (editorVersion.StartsWith("2020") || editorVersion.StartsWith("2021"))
        {
            commands.Add(UnityEditorCommand.Execute($"{k_EditorPath}\\Unity.exe", CreateTestGltfFiles));
        }
        commands.Add(UtrCommand.Run(SystemType.Windows, CreateUtrCommand).ToRetryCommand(2, 20));

        return FluentJob
            .Create($"Performance_{editorVersion}_Win")
            .WithAgent("package-ci/win11:v4", FlavorType.BuildLarge, ResourceType.Vm)
            .WithCommands(commands)
            .WithArtifact("logs", $"{k_ArtifactsPath}/**/*");
    }

    static IJobBuilder CreateAllJob(ISet<IJobBuilder> jobs)
    {
        var deps = new HashSet<Dependency>();
        foreach (var job in jobs)
        {
            deps.Add(new Dependency("Performance", job.Id));
        }

        return FluentJob
            .Create("Performance_All")
            .WithDescription("Runs all performance jobs.")
            .WithDependencies(deps);
    }

    static IUnityEditorExecuteBuilder EnablePerformanceTests(IUnityEditorExecuteBuilder builder) =>
        builder
            .WithProjectPath(k_ProjectPath)
            .WithBatchMode()
            .WithNoGraphics()
            .WithExecuteMethod("GLTFast.Editor.Tests.SetupProject.ApplySetup")
            .WithArgs("glTFastSetup:performance")
            .WithArgs("-upmNoDefaultPackages")
            .WithLogs($"{k_ArtifactsPath}/enable-performance-tests.log")
            .WithQuit();

    static IUnityEditorExecuteBuilder CreateTestGltfFiles(IUnityEditorExecuteBuilder builder) =>
        builder
            .WithProjectPath(k_ProjectPath)
            .WithBatchMode()
            .WithExecuteMethod("GLTFast.Tests.TestGltfGenerator.CreatePerformanceTestFiles")
            .WithNoGraphics()
            .WithArgs("-upmNoDefaultPackages")
            .WithLogs($"{k_ArtifactsPath}/create-gltf-files.log")
            .WithQuit();

    static IUtrCommandBuilder CreateUtrCommand(IUtrCommandBuilder builder) =>
        builder
            .WithSuite(UtrTestSuiteType.Playmode)
            .WithScriptingBackend(ScriptingBackendType.Il2Cpp)
            .WithPlatform(SystemType.Windows)
            .WithEditor(k_EditorPath)
            .WithTestProject(k_ProjectPath)
            .WithArtifacts(k_ArtifactsPath)
            .WithExtraEditorArgs("-upmNoDefaultPackages")
            .WithCategory("Performance")
            .WithPerformanceDataReporting(true)
            .WithPerformanceProject("com.unity.cloud.gltfast");
}
