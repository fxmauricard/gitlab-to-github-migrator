using Octokit;

namespace GitLabToGitHubMigrator;

/// <summary>
/// Manages interactions with the GitHub API for a specific repository.
/// </summary>
/// <param name="owner">The owner of the GitHub repository.</param>
/// <param name="repo">The name of the GitHub repository.</param>
/// <param name="token">The authentication token for accessing the GitHub API.</param>
public class GitHubManager(string owner, string repo, string token)
{
    /// <summary>
    /// Client to interact with the GitHub REST API.
    /// </summary>
    private readonly GitHubClient _client = new(new ProductHeaderValue("GitLabToGitHubMigrator"))
    {
        Credentials = new Credentials(token)
    };

    /// <summary>
    /// 
    /// </summary>
    private readonly Random _random = new();
    
    /// <summary>
    /// The pause duration in milliseconds for secondary rate limits.
    /// </summary>
    private const int SecondaryRateLimitPauseMilliseconds = 1000;

    /// <summary>
    /// The maximum number of retry attempts for API calls.
    /// </summary>
    private const int MaxRetryAttempts = 3;
    
    /// <summary>
    /// Cache for storing milestones retrieved from the GitHub repository.
    /// </summary>
    private List<Milestone> _milestonesCache = [];

    /// <summary>
    /// Creates a new label in the GitHub repository.
    /// </summary>
    /// <param name="label">The label to create.</param>
    public void CreateLabel(NGitLab.Models.Label label)
    {
        Console.Write("\tCreation GitHub : ");
        RetryPolicy(() =>
        {
            // Create a new label object with the name, color, and description from the GitLab label.
            var newLabel = new NewLabel(label.Name, label.Color[1..])
            {
                Description = label.Description
            };

            // Call the GitHub API to create the label in the repository.
            var result = _client.Issue.Labels.Create(owner, repo, newLabel).Result;
            Console.Write(result != null ? "OK." : "-");
        });
    }

    /// <summary>
    /// Creates a new milestone in the GitHub repository.
    /// </summary>
    /// <param name="milestone">The milestone to create.</param>
    public void CreateMilestone(NGitLab.Models.Milestone milestone)
    {
        Console.Write("\tCreation GitHub : ");
        RetryPolicy(() =>
        {
            // Create a new milestone object with the title, description, due date, and state from the GitLab milestone.
            var newMilestone = new NewMilestone(milestone.Title)
            {
                Description = milestone.Description,
                DueOn = milestone.DueDate != null ? DateTimeOffset.Parse(milestone.DueDate) : null,
                State = milestone.State == "active" ? ItemState.Open : ItemState.Closed
            };

            // Call the GitHub API to create the milestone in the repository.
            var result = _client.Issue.Milestone.Create(owner, repo, newMilestone).Result;
            Console.Write(result != null ? "OK." : "-");

            // Invalidate the milestone cache to ensure it is updated with the new milestone.
            InvalidateMilestoneCache();
        });
    }

    /// <summary>
    /// Creates a new issue in the GitHub repository.
    /// </summary>
    /// <param name="issue">The issue to create.</param>
    /// <exception cref="Exception">Thrown when the rate limit is exceeded.</exception>
    public void CreateIssue(NGitLab.Models.Issue issue)
    {
        Console.Write("\tCreation GitHub : ");
        RetryPolicy(() =>
        {
            // Check the current rate limit status.
            var rateLimit = _client.RateLimit.GetRateLimits().Result;
            var coreRateLimit = rateLimit.Resources.Core;

            if (coreRateLimit.Remaining > 0)
            {
                // Create a new issue object with the title and description from the GitLab issue.
                var newIssue = new NewIssue(issue.Title)
                {
                    Body = issue.Description
                };
                if (issue.Milestone != null)
                {
                    newIssue.Milestone = GetMilestoneNumber(issue.Milestone.Title);
                }

                // Add labels to the new issue.
                foreach (var label in issue.Labels)
                {
                    newIssue.Labels.Add(label);
                }

                // Call the GitHub API to create the issue in the repository.
                var result = _client.Issue.Create(owner, repo, newIssue).Result;
                // Pause to avoid hitting secondary rate limits.
                Thread.Sleep(_random.Next((int) (SecondaryRateLimitPauseMilliseconds * 0.9), (int) (SecondaryRateLimitPauseMilliseconds * 5.5)));

                Console.Write(result != null ? "OK" : "-");

                // If the issue is closed in GitLab, close it in GitHub as well.
                if (result != null && issue.State == "closed")
                {
                    CloseIssue(result);
                    Console.Write(" - Closed");
                }

                Console.Write(".");
            }
            else
            {
                // If the rate limit is exceeded, wait until it resets.
                var resetTime = coreRateLimit.Reset.UtcDateTime;
                var delay = resetTime - DateTime.UtcNow;
                Console.Write($"Rate limit exceeded. Waiting for {delay.TotalSeconds} seconds... ");
                Thread.Sleep(delay);

                throw new Exception();
            }
        });
    }

    /// <summary>
    /// Retrieves the milestone number for a given milestone title.
    /// </summary>
    /// <param name="title">The title of the milestone.</param>
    /// <returns>The milestone number, or null if not found.</returns>
    private int? GetMilestoneNumber(string title)
    {
        // If the cache is empty, retrieve all milestones from the GitHub repository.
        if (_milestonesCache.Count == 0)
        {
            var request = new MilestoneRequest
            {
                State = ItemStateFilter.All
            };

            _milestonesCache = _client.Issue.Milestone.GetAllForRepository(owner, repo, request).Result.ToList();
        }

        // Search for the milestone by title in the cache.
        foreach (var milestone in _milestonesCache.Where(milestone => milestone.Title == title))
        {
            return milestone.Number;
        }

        return null;
    }

    /// <summary>
    /// Invalidates the milestone cache.
    /// </summary>
    private void InvalidateMilestoneCache()
    {
        _milestonesCache.Clear();
    }

    /// <summary>
    /// Closes an issue in the GitHub repository.
    /// </summary>
    /// <param name="issue">The issue to close.</param>
    private void CloseIssue(Issue issue)
    {
        // Create an update object to change the state of the issue to closed.
        var update = issue.ToUpdate();
        update.State = ItemState.Closed;

        // Call the GitHub API to update the issue in the repository.
        var result = _client.Issue.Update(owner, repo, issue.Number, update).Result;
        // Pause to avoid hitting secondary rate limits.
        Thread.Sleep(_random.Next((int) (SecondaryRateLimitPauseMilliseconds * 0.9), (int) (SecondaryRateLimitPauseMilliseconds * 5.5)));
    }

    /// <summary>
    /// Handles exceptions that occur during API calls.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    private void HandleException(Exception exception)
    {
        switch (exception.InnerException)
        {
            case RateLimitExceededException rateLimitExceededException:
            {
                // If the rate limit is exceeded, wait until it resets.
                var resetTime = rateLimitExceededException.Reset.UtcDateTime;
                var delay = resetTime - DateTime.UtcNow;
                Console.Write($"Rate limit exceeded. Waiting for {delay.TotalSeconds} seconds... ");
                Thread.Sleep(delay);
                break;
            }
            case SecondaryRateLimitExceededException:
            {
                // If the secondary rate limit is exceeded, wait until it resets.
                var rateLimit = _client.RateLimit.GetRateLimits().Result;
                var resetTime = rateLimit.Resources.Core.Reset.UtcDateTime;
                var delay = resetTime - DateTime.UtcNow;
                Console.Write($"Second rate limit exceeded. Waiting for {delay.TotalSeconds} seconds... ");
                Thread.Sleep(delay);
                break;
            }
            default:
                Console.Write("KO ! (" + exception.InnerException?.Message + ")");
                break;
        }
    }

    /// <summary>
    /// Implements a retry policy for API calls.
    /// </summary>
    /// <param name="action">The action to retry.</param>
    private void RetryPolicy(Action action)
    {
        var attempts = 0;
        var objectCreated = false;

        while (!objectCreated && attempts < MaxRetryAttempts)
        {
            try
            {
                action();
                objectCreated = true;
            }
            catch (Exception exception)
            {
                attempts++;
                if (attempts >= MaxRetryAttempts)
                {
                    Console.Write("Max retry attempts reached. KO !");
                }
                else
                {
                    HandleException(exception);
                }
            }
            finally
            {
                Console.WriteLine();
            }
        }
    }
}
