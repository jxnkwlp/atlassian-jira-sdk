using Atlassian.Jira.Remote;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Atlassian.Jira
{
    /// <summary>
    /// Represents the rendered fields of an issue.
    /// </summary>
    public class IssueRenderedFields
    {
        private readonly IDictionary<string, JToken> _map;

        /// <summary>
        /// Creates a new instance of IssueRenderedFields.
        /// </summary>
        /// <param name="remoteIssue">The remote issue that contains the rendered fields.</param>
        /// <param name="jira">The Jira instance that owns the issue.</param>
        public IssueRenderedFields(RemoteIssue remoteIssue)
        {
            _map = remoteIssue.renderedFieldsReadOnly ?? new Dictionary<string, JToken>();
        }

        /// <summary>
        ///  Gets the field with the specified key.
        /// </summary>
        public JToken this[string key] => _map[key];

        /// <summary>
        /// Determines whether the issue contains a rendered field with the specified key.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return _map.ContainsKey(key);
        }

        /// <summary>
        /// Gets the rendered field associated with the specified key.
        /// </summary>
        public bool TryGetValue(string key, out JToken value)
        {
            return _map.TryGetValue(key, out value);
        }

        /// <summary>
        /// Get the number of elements contained in the IssueRenderedFields
        /// </summary>
        public int Count => _map.Count;
    }
}
