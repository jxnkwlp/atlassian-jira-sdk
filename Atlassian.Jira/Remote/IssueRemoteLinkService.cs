﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Atlassian.Jira.Remote
{
    internal class IssueRemoteLinkService : IIssueRemoteLinkService
    {
        private readonly Jira _jira;

        public IssueRemoteLinkService(Jira jira)
        {
            _jira = jira;
        }

        public Task CreateRemoteLinkAsync(string issueKey, string remoteUrl, string title, string summary, CancellationToken token = default(CancellationToken))
        {
            if (String.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException(nameof(title), "Title must be supplied.");
            }

            if (String.IsNullOrEmpty(remoteUrl))
            {
                throw new ArgumentNullException(nameof(remoteUrl), "Remote URL must be supplied.");
            }

            var serializerSettings = _jira.RestClient.Settings.JsonSerializerSettings;
            var bodyObject = new JObject();
            var bodyObjectContent = new JObject();
            bodyObject.Add("object", bodyObjectContent);

            bodyObjectContent.Add("title", title);
            bodyObjectContent.Add("url", remoteUrl);

            if (!String.IsNullOrEmpty(summary))
            {
                bodyObjectContent.Add("summary", summary);
            }

            var requestBody = JsonConvert.SerializeObject(bodyObject, serializerSettings);

            return _jira.RestClient.ExecuteRequestAsync(Method.Post, String.Format("rest/api/2/issue/{0}/remotelink", issueKey), requestBody, token);
        }

        public async Task<IEnumerable<IssueRemoteLink>> GetRemoteLinksForIssueAsync(string issueKey, CancellationToken token = default(CancellationToken))
        {
            var serializerSettings = _jira.RestClient.Settings.JsonSerializerSettings;
            var resource = String.Format("rest/api/2/issue/{0}/remotelink", issueKey);
            var remoteLinksJson = await _jira.RestClient.ExecuteRequestAsync(Method.Get, resource, null, token).ConfigureAwait(false);

            var links = remoteLinksJson.Cast<JObject>();
            var result = links.Select(json =>
            {
                var objJson = json["object"];
                var title = objJson["title"]?.Value<string>();
                var url = objJson["url"].Value<string>();
                var summary = objJson["summary"]?.Value<string>();
                return new IssueRemoteLink(url, title, summary);
            });
            return result;
        }
    }
}
