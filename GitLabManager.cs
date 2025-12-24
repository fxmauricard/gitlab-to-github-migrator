using NGitLab;

namespace GitLabToGitHubMigrator;

/// <summary>
/// Manages interactions with the GitLab API for a specific project.
/// </summary>
/// <param name="url">The URL of the GitLab API.</param>
/// <param name="authenticationToken">The authentication token for accessing the GitLab API.</param>
/// <param name="projectId">The ID of the GitLab project.</param>
public class GitLabManager(string url, string authenticationToken, int projectId)
{
    /// <summary>
    /// Client to interact with the GitLab REST API.
    /// </summary>
    private readonly GitLabClient _client = new(url, authenticationToken);

    /// <summary>
    /// Retrieves labels from the GitLab project.
    /// </summary>
    /// <returns>A list of labels from the GitLab project.</returns>
    public List<NGitLab.Models.Label> GetLabels()
    {
        return _client.Labels.ForProject(projectId).ToList();
    }

    /// <summary>
    /// Retrieves milestones from the GitLab project.
    /// </summary>
    /// <returns>A list of milestones from the GitLab project.</returns>
    public List<NGitLab.Models.Milestone> GetMilestones()
    {
        return _client.GetMilestone(projectId).All.ToList();
    }

    /// <summary>
    /// Retrieves issues from the GitLab project.
    /// </summary>
    /// <returns>A list of issues from the GitLab project.</returns>
    public List<NGitLab.Models.Issue> GetIssues()
    {
        return _client.Issues.ForProject(projectId).ToList();
    }
}
