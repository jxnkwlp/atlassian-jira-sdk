using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class IssueFieldService : IIssueFieldService
    {
        private readonly Jira _jira;

        public IssueFieldService(Jira jira)
        {
            _jira = jira;
        }

        public async Task<IEnumerable<CustomField>> GetCustomFieldsAsync(CancellationToken token = default(CancellationToken))
        {
            var cache = _jira.Cache;

            if (!cache.CustomFields.Any())
            {
                var remoteFields = await _jira.RestClient.ExecuteRequestAsync<RemoteField[]>(Method.Get, "rest/api/2/field", null, token).ConfigureAwait(false);
                var results = remoteFields.Where(f => f.IsCustomField).Select(f => new CustomField(f));
                cache.CustomFields.TryAdd(results);
            }

            return cache.CustomFields.Values;
        }


        /// <summary>
        /// Note: https://confluence.atlassian.com/jiracore/createmeta-rest-endpoint-to-be-removed-975040986.html
        /// Function updated to use new API call
        /// CustomFieldFetchOptions now accepts a signle Project Key and a single Issue Type Id
        /// When Issue Type Id is empty we are going to receive all CustomFields for the given Project
        /// </summary>
        public async Task<IEnumerable<CustomField>> GetCustomFieldsAsync(CustomFieldFetchOptions options, CancellationToken token = default(CancellationToken))
        {
            var cache = _jira.Cache;
            var projectIdOrKey = options.ProjectKey;
            var projectKey = options.ProjectKey;
            var issueTypeId = options.IssueTypeId;

            if (!string.IsNullOrEmpty(issueTypeId) || !string.IsNullOrEmpty(issueTypeId))
            {
                projectKey = $"{projectKey}::{issueTypeId}";
            }
            else if (string.IsNullOrEmpty(projectKey))
            {
                return await GetCustomFieldsAsync(token);
            }
            else if (string.IsNullOrEmpty(issueTypeId))
            {
                IEnumerable<IssueType> issueTypeIds;
                List<CustomField> projectCustomFields = new List<CustomField>();

                try
                {
                    issueTypeIds = await _jira.IssueTypes.GetIssueTypesForProjectAsync(projectIdOrKey).ConfigureAwait(false);
                }
                catch (ResourceNotFoundException)
                {
                    throw new InvalidOperationException($"Project with key '{projectIdOrKey}' was not found on the Jira server.");
                }

                foreach (var i in issueTypeIds)
                {
                    var opt = new CustomFieldFetchOptions() { ProjectKey = projectIdOrKey, IssueTypeId = i.Id };
                    var customFields = await GetCustomFieldsAsync(opt, token);
                    projectCustomFields.AddRange(customFields);
                }

                return projectCustomFields.GroupBy(c => c.Id).Select(g => g.First());
            }

            if (!cache.ProjectCustomFields.TryGetValue(projectKey, out JiraEntityDictionary<CustomField> fields))
            {
                var resource = $"rest/api/2/issue/createmeta/{projectIdOrKey}/issuetypes/{issueTypeId}";
                JToken jProject = null;

                try
                {
                    var jToken = await _jira.RestClient.ExecuteRequestAsync(Method.Get, resource, null, token).ConfigureAwait(false);
                    jProject = jToken["values"];
                }
                catch (ResourceNotFoundException)
                {
                    throw new InvalidOperationException($"Project with key '{projectIdOrKey}' was not found on the Jira server.");
                }

                var serializerSettings = _jira.RestClient.Settings.JsonSerializerSettings;
                var customFields = jProject.SelectMany(issueType => GetCustomFieldsFromIssueType(issueType, serializerSettings));
                var distinctFields = customFields.GroupBy(c => c.Id).Select(g => g.First());

                cache.ProjectCustomFields.TryAdd(projectKey, new JiraEntityDictionary<CustomField>(distinctFields));
            }

            return cache.ProjectCustomFields[projectKey].Values;
        }


        public Task<IEnumerable<CustomField>> GetCustomFieldsForProjectAsync(string projectKey, CancellationToken token = default(CancellationToken))
        {
            var options = new CustomFieldFetchOptions
            {
                ProjectKey = projectKey
            };

            return GetCustomFieldsAsync(options, token);
        }

        private static IEnumerable<CustomField> GetCustomFieldsFromIssueType(JToken issueType, JsonSerializerSettings serializerSettings)
        {
            var remoteField = JsonConvert.DeserializeObject<RemoteField>(issueType.ToString(), serializerSettings);

            // map fieldId to id
            remoteField.id = issueType["fieldId"].ToString();

            if (!remoteField.id.StartsWith("customfield_", StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<CustomField>();

            // there is no custom property returned by createmeta API, since we have an customfield id, mark it as custom
            remoteField.IsCustomField = true;

            return new[] { new CustomField(remoteField) };
        }
    }
}
