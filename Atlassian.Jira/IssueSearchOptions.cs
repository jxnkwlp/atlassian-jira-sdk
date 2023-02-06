using System.Collections.Generic;

namespace Atlassian.Jira
{
    /// <summary>
    /// Options when performing an issue search.
    /// </summary>
    public class IssueSearchOptions
    {
        /// <summary>
        /// Creates a new instance of IssueSearchOptions.
        /// </summary>
        /// <param name="jql">The JQL of the search to execute.</param>
        public IssueSearchOptions(string jql)
        {
            this.Jql = jql;
        }

        /// <summary>
        /// The JQL of the search to execute.
        /// </summary>
        public string Jql { get; private set; }

        /// <summary>
        /// Maximum number of issues to return (defaults to the value of Jira.Issues.MaxIssuesPerRequest).
        /// </summary>
        public int? MaxIssuesPerRequest { get; set; }

        /// <summary>
        /// Index of the first issue to return (0-based).
        /// </summary>
        public int StartAt { get; set; } = 0;

        /// <summary>
        /// Whether to validate a JQL query.
        /// </summary>
        public bool ValidateQuery { get; set; } = true;

        /// <summary>
        /// Whether to automatically include all fields exposed by the Issue class in the response.
        /// </summary>
        public bool FetchBasicFields { get; set; } = true;

        /// <summary>
        /// Additional fields to include as part of the response.
        /// </summary>
        public IList<string> AdditionalFields { get; set; } = new List<string>();

        /// <summary>
        /// The fields of the Issue object to expand with information on the server.
        /// See https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-search/#api-rest-api-3-search-get
        /// </summary>
        /// <remarks>To return the description as html you can use the "renderedFields" flag.</remarks>
        public IList<string> Expand { get; set; } = new List<string>();
    }
}
