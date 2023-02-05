using System.Collections.Generic;

namespace Atlassian.Jira
{
    /// <summary>
    /// Represents the filters to use when fetching custom fields.
    /// </summary>
    public class CustomFieldFetchOptions
    {
        /// <summary>
        /// The project with which to filter the results.
        /// </summary>
        public string ProjectKey { get; set; }

        /// <summary>
        /// The issue type id with which to filter the results.
        /// If IssueTypeId is empty we are going to fetch all
        /// available issue types for the given project
        /// </summary>
        public string IssueTypeId { get; set; }

    }
}
