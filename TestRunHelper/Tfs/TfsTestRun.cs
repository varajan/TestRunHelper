using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace TestRunHelper.Tfs
{
    public class TfsTestRun
    {
        private readonly string Team = System.Configuration.ConfigurationSettings.AppSettings["Team"];
        private readonly string Url = System.Configuration.ConfigurationSettings.AppSettings["TfsUrl"];
        private readonly string Usr = System.Configuration.ConfigurationSettings.AppSettings["login"];
        private readonly string Pwd = System.Configuration.ConfigurationSettings.AppSettings["password"];

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
                    catch (Exception)
                    {
                        // nothing
                    }
                }

                if (_collection == null || !_collection.HasAuthenticated)
                {
                    _collection = null;
                    throw new TeamFoundationServerUnauthorizedException("Connection to TFS is not established.");
                }

                return _collection;
            }
        }

        private ITestManagementTeamProject _teamProject;
        public ITestManagementTeamProject TeamProject => _teamProject =
            _teamProject ?? Collection.GetService<ITestManagementService>().GetTeamProject(Team);


        private List<ITestRun> _testRuns;
        public List<ITestRun> TestRuns => _testRuns = _testRuns ??
             TeamProject.TestRuns.Query("select * from TestRun")
                .Where(run => run.LastUpdated > DateTime.UtcNow.AddDays(-10))
                .Where(run => run.Title.Contains("VSTest Test Run"))
                .ToList();

        public List<ITestCaseResult> TestCaseResults(int runId) => TeamProject.TestRuns.Find(runId).QueryResults().ToList();
    }
}