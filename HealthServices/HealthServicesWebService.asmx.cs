using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;
using System.Xml.Linq;

namespace HealthServices
{
    /// <summary>
    /// Summary description for HealthServicesWebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class HealthServicesWebService : System.Web.Services.WebService
    {
        [WebMethod]
        public List<string> ListOfProjectNamesThatFailedValidation(string validationLogText)
        {
            List<string> projectsThatFailedValidation = new List<string>();
            try
            {
                List<string> tokenizedLog = validationLogText.Replace("[Error", "~[Error").Split('~').ToList();
                foreach (string error in tokenizedLog)
                {
                    string[] errorSections = error.Replace(".zip", "~").Split('~');
                    if (errorSections.Count() >= 2)
                    {
                        string projectName = errorSections[errorSections.Count() - 2].Split('\\').Last();
                        if (!projectsThatFailedValidation.Contains(projectName))
                        {
                            projectsThatFailedValidation.Add(projectName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return projectsThatFailedValidation;
        }

        [WebMethod]
        public List<Project> AuthorListByProject(string collectionName, string personalAccessToken)
        {
            List<Project> authorListByProject = new List<Project>();

            List<string> projectList = ListProjects(collectionName, personalAccessToken);
            foreach (string projectName in projectList)
            {
                List<string> changeSetList = ListChangeSets(collectionName, projectName, personalAccessToken);
                Project currentProject = new Project(changeSetList, projectName);
                authorListByProject.Add(currentProject);
            }

            return authorListByProject;
        }

        [WebMethod]
        public List<string> PipeDelimitedAuthorListByProjectInCollection(string collectionName, string personalAccessToken)
        {
            List<string> authorListByProject = new List<string>();

            List<string> projectList = ListProjects(collectionName, personalAccessToken);
            foreach (string projectName in projectList)
            {
                string authorList = ListChangeSetsPipeDelimited(collectionName, projectName, personalAccessToken);
                authorListByProject.Add(authorList);
            }

            return authorListByProject;
        }

        [WebMethod]
        public string ListChangeSetsPipeDelimited(string collectionName, string projectName, string personalAccessToken)
        {
            List<string> authorList = ListChangeSets(collectionName, projectName, personalAccessToken);
            string httpResponse = "";
            foreach (string authorName in authorList)
            {
                httpResponse += authorName + "|";
            }
            return httpResponse.TrimEnd('|');
        }

        [WebMethod]
        public List<string> ListChangeSets(string collectionName, string projectName, string personalAccessToken)
        {
            Task<string> task = Task.Run<string>(async () => await GetChangeSets(collectionName, projectName, personalAccessToken));
            string httpResponseBody = task.Result;

            List<string> authorList = new List<string>();
            try
            {
                JObject details = JObject.Parse(httpResponseBody);
                JToken root = details.Root.Last();
                JEnumerable<JToken> children = root.Children();
                foreach (JToken child in children.FirstOrDefault())
                {
                    string value = child["author"]["displayName"].ToString();
                    if(!authorList.Contains(value))
                    {
                        authorList.Add(value);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return authorList;
        }

        [WebMethod]
        public List<string> ListProjects(string collectionName, string personalAccessToken)
        {
            Task<string> task = Task.Run<string>(async () => await GetProjects(collectionName, personalAccessToken));
            string httpResponseBody = task.Result;

            List<string> projectList = new List<string>();
            try
            {
                JObject details = JObject.Parse(httpResponseBody);
                JToken root = details.Root.Last();
                JEnumerable<JToken> children = root.Children();
                foreach (JToken child in children.FirstOrDefault())
                {
                    string value = child["name"].ToString();
                    if (!projectList.Contains(value))
                    {
                        projectList.Add(value);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return projectList;
        }

        [WebMethod]
        public string DisplayProcessTemplate(string collectionName, string personalAccessToken)
        {
            Task<string> task = Task.Run<string>(async () => await CheckProcessTemplate(collectionName, personalAccessToken));
            string httpResponseBody = task.Result;

            return httpResponseBody;
        }

        public static async Task<string> CheckProcessTemplate(string collectionName, string personalAccessToken)
        {
            string responseBody = "";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", personalAccessToken))));

                    using (HttpResponseMessage response = await client.GetAsync(
                                "http://tfs.interiorhealth.ca:8080/tfs/" + collectionName + "/_apis/work/processadmin/processes/checktemplateexistence/"))
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return responseBody;
        }

        public static async Task<string> GetProjects(string collectionName, string personalAccessToken)
        {
            string responseBody = "";
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", personalAccessToken))));

                    using (HttpResponseMessage response = await client.GetAsync(
                                "http://tfs.interiorhealth.ca:8080/tfs/" + collectionName + "/_apis/projects"))
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return responseBody;
        }

        public static async Task<string> GetChangeSets(string collectionName, string projectName, string personalAccessToken)
        {
            if(!string.IsNullOrWhiteSpace(projectName))
            {
                collectionName += "/" + projectName;
            }
            string responseBody = "no response";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", personalAccessToken))));

                    using (HttpResponseMessage response = await client.GetAsync(
                                "http://tfs.interiorhealth.ca:8080/tfs/" + collectionName + "/_apis/tfvc/changesets/"
                                ))
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return responseBody;
        }

        public class Project
        {
            public string name;
            public List<string> contributors;

            public Project()
            {
                name = "";
                contributors = new List<string>(); ;
            }

            public Project(List<string> newContributorList, string newName = "")
            {
                name = newName;
                contributors = newContributorList;
            }
        }
    }
}
