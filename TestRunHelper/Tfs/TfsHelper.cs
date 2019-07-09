using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using TestRunHelper.Helpers;

namespace TestRunHelper.Tfs
{
    public class TfsHelper
    {
        private static string Definition => ConfigurationManager.AppSettings["Definition"];
        private static string Team => ConfigurationManager.AppSettings["Team"];
        private static string Url => ConfigurationManager.AppSettings["TfsUrl"];
        private static string Usr => ConfigurationManager.AppSettings["login"];
        private static string Pwd => ConfigurationManager.AppSettings["password"];

        private TfsTeamProjectCollection _collection;
        public TfsTeamProjectCollection Collection
        {
            get
            {
                if (_collection == null || !_collection.HasAuthenticated)
                {
                    try
                    {
                        _collection = new TfsTeamProjectCollection(new Uri(Url), new System.Net.NetworkCredential(Usr, Pwd));
                        _collection.Authenticate();
                    }
                    catch (Exception exception)
                    {
                        Logger.Error("Failed to connect to TFS.");
                        Logger.Error(exception);
                    }
                }

                if (_collection == null || !_collection.HasAuthenticated)
                {
                    _collection = null;
                    var error = "Connection to TFS is not established. " +
                                "Maybe you should specify or update credentials in the config file.";

                    Logger.Error(error);
                    throw new TeamFoundationServerUnauthorizedException(error);
                }

                Logger.Info("Connection to TFS is ok");
                return _collection;
            }
        }

        private ITestManagementTeamProject _teamProject;
        public ITestManagementTeamProject TeamProject => _teamProject =
            _teamProject ?? Collection.GetService<ITestManagementService>().GetTeamProject(Team);

        private BuildHttpClient _buildClient;
        public BuildHttpClient BuildClient => _buildClient = _buildClient ?? Collection.GetClient<BuildHttpClient>();

        public ITestRun GetTestRun(int id) => TeamProject.TestRuns.Find(id);

        private List<ITestRun> _testRuns;
        public List<ITestRun> TestRuns => _testRuns = _testRuns ??
             TeamProject.TestRuns.Query("select * from TestRun")
                .Where(run => run.LastUpdated > DateTime.UtcNow.AddMonths(-1))
                .Where(run => run.Title.Contains("VSTest Test Run"))
                .ToList();

        public Build GetBuild(string buildNumber) => Builds.First(build => build.BuildNumber.Equals(buildNumber));

        public ITestRun GetTestRun(string buildNumber) =>
            TeamProject.TestRuns.ByBuild(GetBuild(buildNumber).Uri).FirstOrDefault();

        public List<ITestCaseResult> TestCaseResults(int runId) => TeamProject.TestRuns.Find(runId).QueryResults().ToList();

        public List<Build> Builds => BuildClient.GetBuildsAsync(Team).Result
                                        .Where(build => build.Definition.Name.Equals(Definition))
                                        .Where(build => build.FinishTime > DateTime.Today.AddMonths(-1))
                                        .ToList();
    }
}