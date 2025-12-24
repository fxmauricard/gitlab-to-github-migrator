using System.Configuration;

namespace GitLabToGitHubMigrator;

/// <summary>
/// The main application class responsible for migrating data from GitLab to GitHub.
/// </summary>
internal static class Application
{
    // Variables retrieved from the configuration file.
    // These variables store the necessary configuration settings for connecting to GitLab and GitHub.
    private static readonly string GitLabUrl = ConfigurationManager.AppSettings["GITLAB_URL"] ?? string.Empty;
    private static readonly string GitLabToken = ConfigurationManager.AppSettings["GITLAB_TOKEN"] ?? string.Empty;
    private static readonly int GitLabProjectId = int.Parse(ConfigurationManager.AppSettings["GITLAB_PROJECT_ID"] ?? throw new InvalidOperationException());
    private static readonly string GitHubOwner = ConfigurationManager.AppSettings["GITHUB_OWNER"] ?? string.Empty;
    private static readonly string GitHubRepo = ConfigurationManager.AppSettings["GITHUB_REPO"] ?? string.Empty;
    private static readonly string GitHubToken = ConfigurationManager.AppSettings["GITHUB_TOKEN"] ?? string.Empty;

    // Instances of API managers.
    // These instances are used to interact with GitLab and GitHub APIs.
    private static readonly GitLabManager GitLabManager = new(GitLabUrl, GitLabToken, GitLabProjectId);
    private static readonly GitHubManager GitHubManager = new(GitHubOwner, GitHubRepo, GitHubToken);

    /// <summary>
    /// The main entry point of the application.
    /// </summary>
    public static void Main()
    {
        // Display configuration settings.
        DisplayConfiguration();

        // Process labels.
        ProcessItems("Labels", GitLabManager.GetLabels, l => l.Name, GitHubManager.CreateLabel);

        // Process milestones.
        ProcessItems("Milestones", GitLabManager.GetMilestones, m => m.Title, GitHubManager.CreateMilestone);

        // Process issues.
        ProcessItems("Issues", GitLabManager.GetIssues, i => i.Title, GitHubManager.CreateIssue);
    }

    /// <summary>
    /// Displays the configuration settings.
    /// This method prints the configuration settings to the console for verification.
    /// </summary>
    private static void DisplayConfiguration()
    {
        Console.WriteLine("GitLabToGitHubMigrator");
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine($"* GITLAB_URL: {GitLabUrl}");
        Console.WriteLine($"* GITLAB_PROJECT_ID: {GitLabProjectId}");
        Console.WriteLine($"* GITHUB_OWNER: {GitHubOwner}");
        Console.WriteLine($"* GITHUB_REPO: {GitHubRepo}");
        Console.WriteLine($"* GITLAB_TOKEN: {GitLabToken}");
        Console.WriteLine($"* GITHUB_TOKEN: {GitHubToken}");
        Console.WriteLine();
    }

    /// <summary>
    /// Processes and migrates items from GitLab to GitHub.
    /// This method retrieves items from GitLab and processes them in GitHub.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="itemType">The type of items being processed (e.g., "Labels", "Milestones", "Issues").</param>
    /// <param name="retrieveItemsFunc">A function to retrieve items from GitLab.</param>
    /// <param name="itemLabelFunc">A function to get the label of the item.</param>
    /// <param name="processItem">A function to process items in GitHub.</param>
    private static void ProcessItems<T>(string itemType, Func<List<T>> retrieveItemsFunc, Func<T, string> itemLabelFunc, Action<T> processItem)
    {
        Console.WriteLine($"[{itemType}]");

        // Retrieve items from GitLab.
        var items = retrieveItemsFunc();
        Console.WriteLine("Count: " + items.Count);
        Console.WriteLine();

        // Iterate through each item and create it in GitHub.
        foreach (var item in items)
        {
            Console.WriteLine($"{itemType[..^1]}: [{itemLabelFunc(item)}]");
            processItem(item);
        }
    }
}
