# Copilot Instructions for GitLabToGitHubMigrator

## Project Overview

GitLabToGitHubMigrator is a .NET 9.0 console application that migrates GitLab repositories (labels, milestones, and issues) to GitHub using the GitLab and GitHub APIs.

## Technology Stack

- **Language**: C# 13
- **Framework**: .NET 9.0
- **Key Dependencies**:
  - `NGitLab` (v6.57.0) - GitLab API client
  - `Octokit` (v13.0.1) - GitHub API client
  - `System.Configuration.ConfigurationManager` (v9.0.0) - Configuration management

## Build and Test Commands

- **Build the project**: `dotnet build`
- **Run the application**: `dotnet run`
- **Run tests**: `dotnet test`
- **Restore dependencies**: `dotnet restore` or `msbuild /t:Restore`

The project uses both dotnet CLI and MSBuild. The CI workflow in `.github/workflows/dotnet-desktop.yml` runs on Windows and executes tests and restoration using both tools.

## Project Structure

- `Program.cs` - Main application entry point and migration orchestration logic
- `GitLabManager.cs` - Handles all GitLab API interactions
- `GitHubManager.cs` - Handles all GitHub API interactions, including retry logic and rate limit handling
- `GitLabToGitHubMigrator.csproj` - Project configuration file
- `global.json` - SDK version configuration (v9.0.0)

## Coding Conventions and Style

### General Conventions

- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled
- **Language version**: C# 13
- **Documentation**: Use XML documentation comments (`///`) for all public classes and methods

### Naming Conventions

- Use PascalCase for class names, method names, and properties
- Use camelCase with underscore prefix for private fields (e.g., `_client`, `_milestonesCache`)
- Use PascalCase for constants (e.g., `SecondaryRateLimitPauseMilliseconds`, `MaxRetryAttempts`)

### Code Organization

- Keep API client instances as private readonly fields
- Use primary constructors for dependency injection (C# 12+ feature)
- Group related functionality in dedicated manager classes
- Use generic methods when appropriate (e.g., `ProcessItems<T>`)

### API Interaction Patterns

1. **Retry Logic**: All GitHub API calls should use the `RetryPolicy` method to handle transient failures
2. **Rate Limiting**: 
   - Check rate limits before making requests
   - Implement exponential backoff and waiting when limits are hit
   - Add random delays between requests to avoid secondary rate limits
3. **Exception Handling**: Use `HandleException` method to properly handle GitLab/GitHub specific exceptions
4. **Caching**: Use local caching for frequently accessed data (e.g., milestones)

### Console Output Conventions

- Use `Console.WriteLine()` for section headers and completion messages
- Use `Console.Write()` for inline status updates
- Format output consistently:
  - `[ItemType]` for section headers
  - `ItemType: [ItemName]` for individual items
  - `\tCreation GitHub : ` followed by status indicators:
    - `OK.` for successful label/milestone creation
    - `OK` for successful issue creation (without period)
    - `KO ! (error message)` for failures
    - `-` for null results

## Configuration

The application uses `ConfigurationManager.AppSettings` with the following required settings:

- `GITLAB_URL` - GitLab API endpoint
- `GITLAB_TOKEN` - GitLab authentication token
- `GITLAB_PROJECT_ID` - GitLab project ID
- `GITHUB_OWNER` - GitHub repository owner
- `GITHUB_REPO` - GitHub repository name
- `GITHUB_TOKEN` - GitHub authentication token

## Key Patterns

### Functional Programming Approach

The codebase uses functional patterns for migration operations:
```csharp
ProcessItems<T>(string itemType, Func<List<T>> retrieveFunc, Func<T, string> labelFunc, Action<T> processAction)
```

### Error Handling

- Use try-catch blocks within retry policies
- Log errors with descriptive messages
- Continue processing even if individual items fail

### State Management

- Milestone cache is invalidated after creation to ensure consistency
- Thread.Sleep is used for rate limiting (not async/await)

## Important Notes

- The application uses synchronous API calls with `.Result` (blocking pattern)
- Thread.Sleep is used for delays, not async/await
- Random delays are applied to API calls to avoid hitting secondary rate limits
- GitHub colors require the '#' prefix to be removed (GitLab format: `#RRGGBB`, GitHub format: `RRGGBB`)
