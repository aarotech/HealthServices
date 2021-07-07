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

            List<string> projectList = ListProjectNames(collectionName, personalAccessToken);
            foreach (string projectName in projectList)
            {
                List<string> changeSetList = ListAuthors(collectionName, projectName, personalAccessToken);
                Project currentProject = new Project(changeSetList, projectName);
                authorListByProject.Add(currentProject);
            }

            return authorListByProject;
        }

        [WebMethod]
        public List<string> PipeDelimitedAuthorListByProjectInCollection(string collectionName, string personalAccessToken)
        {
            List<string> authorListByProject = new List<string>();

            List<string> projectList = ListProjectNames(collectionName, personalAccessToken);
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
            List<string> authorList = ListAuthors(collectionName, projectName, personalAccessToken);
            string httpResponse = "";
            foreach (string authorName in authorList)
            {
                httpResponse += authorName + "|";
            }
            return httpResponse.TrimEnd('|');
        }

        [WebMethod]
        public List<string> ListAuthors(string collectionName, string projectName, string personalAccessToken)
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
                    if (!authorList.Contains(value))
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
        public List<string> ListProjectNames(string collectionName, string personalAccessToken)
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
        public List<Project> ProjectDetailsByProjectForCollection(string collectionName, string personalAccessToken)
        {
            List<Project> projectDetailsList = new List<Project>();
            List<string> projectList = ListProjectNames(collectionName, personalAccessToken);
            foreach (string projectNameString in projectList)
            {
                List<WorkItemType> workItemTypeCategories = ListStatesByWorkItemType(collectionName, projectNameString, personalAccessToken);
                List<string> contributorList = ListAuthors(collectionName, projectNameString, personalAccessToken);
                Project currentProject = new Project(contributorList, workItemTypeCategories, projectNameString);
                projectDetailsList.Add(currentProject);
            }

            return projectDetailsList;
        }

        [WebMethod]
        public List<Project> WorkItemTypeCategoriesByProject(string collectionName, string personalAccessToken)
        {
            List<Project> workItemTypeCategoriesByProject = new List<Project>();

            List<string> projectList = ListProjectNames(collectionName, personalAccessToken);
            foreach (string projectName in projectList)
            {
                List<string> workItemTypeCategoryList = ListWorkItemTypeCategories(collectionName, projectName, personalAccessToken);
                Project currentProject = new Project(workItemTypeCategoryList, projectName);
                workItemTypeCategoriesByProject.Add(currentProject);
            }

            return workItemTypeCategoriesByProject;
        }

        [WebMethod]
        public List<WorkItemType> ListStatesByWorkItemType(string collectionName, string projectName, string personalAccessToken)
        {
            List<WorkItemType> statesByWorkItemTypeCategory = new List<WorkItemType>();

            List<string> workItemTypeCategoryList = ListWorkItemTypeCategories(collectionName, projectName, personalAccessToken);
            foreach (string workItemTypeName in workItemTypeCategoryList)
            {
                List<string> workItemStateList = ListWorkItemStates(collectionName, projectName, workItemTypeName, personalAccessToken);
                WorkItemType currentWorkItemType = new WorkItemType(workItemTypeName, workItemStateList);
                statesByWorkItemTypeCategory.Add(currentWorkItemType);
            }

            return statesByWorkItemTypeCategory;
        }

        [WebMethod]
        public List<Project> ListWorkItemStatesByProjectForCategory(string collectionName, string workItemCategory, string personalAccessToken)
        {
            List<Project> workItemStates = new List<Project>();
            List<string> projectNames = ListProjectNames(collectionName, personalAccessToken);
            foreach (string projectName in projectNames)
            {
                WorkItemType workItemType = new WorkItemType(workItemCategory, ListWorkItemStates(collectionName, projectName, workItemCategory, personalAccessToken));
                List<WorkItemType> workItemTypes = new List<WorkItemType>();
                workItemTypes.Add(workItemType);
                Project currentProject = new Project(null, workItemTypes, projectName);
                workItemStates.Add(currentProject);
            }

            return workItemStates;
        }

        [WebMethod]
        public List<string> ListWorkItemStates(string collectionName, string projectName, string workItemCategory, string personalAccessToken)
        {
            Task<string> task = Task.Run<string>(async () => await GetWorkItemStates(collectionName, projectName, workItemCategory, personalAccessToken));
            string httpResponseBody = task.Result;

            List<string> workItemStateList = new List<string>();
            try
            {
                JObject details = JObject.Parse(httpResponseBody);
                JToken root = details.Root.Last();
                JEnumerable<JToken> children = root.Children();
                foreach (JToken child in children.FirstOrDefault())
                {
                    string value = child["name"].ToString();
                    if (!workItemStateList.Contains(value))
                    {
                        workItemStateList.Add(value);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return workItemStateList;
        }

        [WebMethod]
        public List<string> ListWorkItemTypeCategories(string collectionName, string projectName, string personalAccessToken)
        {
            Task<string> task = Task.Run<string>(async () => await GetWorkItemTypeCategories(collectionName, projectName, personalAccessToken));
            string httpResponseBody = task.Result;

            List<string> workItemTypeCategoryList = new List<string>();
            try
            {
                JObject details = JObject.Parse(httpResponseBody);
                JToken root = details.Root.Last();
                JEnumerable<JToken> children = root.Children();
                foreach (JToken child in children.FirstOrDefault())
                {
                    string value = child["name"].ToString().Replace(" Category", "");
                    if (!workItemTypeCategoryList.Contains(value))
                    {
                        workItemTypeCategoryList.Add(value);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return workItemTypeCategoryList;
        }

        public static async Task<string> GetProjectProperties(string collectionName, string projectUUID, string personalAccessToken)
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
                                "http://tfs.interiorhealth.ca:8080/tfs/" + collectionName + "/_apis/projects/" + projectUUID + "/properties"))
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

        public static async Task<string> GetWorkItemStates(string collectionName, string projectName, string workItemTypeCategory, string personalAccessToken)
        {
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                collectionName += "/" + projectName;
            }
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
                                "http://tfs.interiorhealth.ca:8080/tfs/" + collectionName + "/_apis/wit/workitemtypes/" + workItemTypeCategory + "/states/"))
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

        public static async Task<string> GetWorkItemTypeCategories(string collectionName, string projectName, string personalAccessToken)
        {
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                collectionName += "/" + projectName;
            }
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
                                "http://tfs.interiorhealth.ca:8080/tfs/" + collectionName + "/_apis/wit/workitemtypecategories/"))
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
            if (!string.IsNullOrWhiteSpace(projectName))
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

        public class WorkItemType
        {
            public string category;
            public List<string> states;

            public WorkItemType()
            {
                category = "";
                states = new List<string>(); ;
            }

            public WorkItemType(string newName = "", List<string> newStateList = null)
            {
                category = newName;
                states = newStateList;
            }
        }

        public class Project
        {
            public string name;
            public List<string> contributors;
            public List<string> workItemTypeCatagories;
            public List<WorkItemType> workItemTypes;

            public Project()
            {
                name = "";
                contributors = new List<string>(); ;
                workItemTypeCatagories = new List<string>(); ;
            }

            public Project(List<string> newContributorList = null, string newName = "", List<string> newWorkItemTypeCatagories = null)
            {
                name = newName;
                contributors = newContributorList;
                workItemTypeCatagories = newWorkItemTypeCatagories;
            }

            public Project(List<string> newContributorList = null, List<WorkItemType> newWorkItemTypes = null, string newName = "")
            {
                name = newName;
                contributors = newContributorList;
                workItemTypes = newWorkItemTypes;
            }
        }
    }
}
