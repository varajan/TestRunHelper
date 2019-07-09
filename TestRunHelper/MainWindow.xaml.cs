using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.Win32;
using TestRunHelper.Helpers;
using TestRunHelper.Tfs;

namespace TestRunHelper
{
    public partial class MainWindow
    {
        private const string TestRunsFolder = "TestRunsFolder";
        private const string SolutionTestsFile = "solutionTests.txt";

        private string BasePlaylistNameFile { get; set; }
        private string PassedTestsFile => $"{TestRunsFolder}\\{BuildNumber}_passed.txt";
        private string FailedTestsFile => $"{TestRunsFolder}\\{BuildNumber}_failed.txt";

        private string PlaylistName
        {
            get
            {
                var result = BuildNumber;

                if (PassedState) result += "_passed";
                if (FailedState) result += "_failed";
                if (InconclusiveState) result += "_inconclusive";

                return result;

            }
        }

        private bool PassedState => Passed.IsChecked.HasValue && Passed.IsChecked.Value;
        private bool FailedState => Failed.IsChecked.HasValue && Failed.IsChecked.Value;
        private bool InconclusiveState => Inconclusive.IsChecked.HasValue && Inconclusive.IsChecked.Value;

        private bool TestRunPassed => $"{TestRuns.SelectedItem}".Split('-').First().Contains("Succeeded");
        private string BuildNumber => $"{TestRuns.SelectedItem}".Split('-').Second().Trim();

        private readonly TfsHelper _tfsHelpers;

        private readonly TestOutcome[] _passedOutcomes = {
            TestOutcome.Passed
        };
        private readonly TestOutcome[] _failedOutcomes = {
            TestOutcome.Failed,
            TestOutcome.Aborted,
            TestOutcome.Error,
            TestOutcome.Timeout,
            TestOutcome.MaxValue
        };
        private readonly TestOutcome[] _incompleteOutcomes = {
            TestOutcome.Inconclusive,
            TestOutcome.NotApplicable,
            TestOutcome.NotExecuted,
            TestOutcome.InProgress,
            TestOutcome.Blocked,
            TestOutcome.Warning,
            TestOutcome.None
        };

        public MainWindow()
        {
            InitializeComponent();

            _tfsHelpers = new TfsHelper();

            TestRuns.IsEnabled = false;
            Passed.IsEnabled = false;
            Failed.IsEnabled = false;
            Inconclusive.IsEnabled = false;

            BasePlaylistName.Visibility = Visibility.Hidden;
            SolutionTests.Visibility = Visibility.Hidden;
            TestRunTests.Visibility = Visibility.Hidden;
            SaveBtn.IsEnabled = false;

            Passed.Click += CheckBoxClick;
            Failed.Click += CheckBoxClick;
            Inconclusive.Click += CheckBoxClick;
            SaveBtn.Click += SavePlaylist;
            Reload.Click += ReloadTestRuns;
            TestRuns.SelectionChanged += UpdateTestsCount;
            SolutionTests.Click += GetSolutionTests;
            TestRunTests.Click += GetTestsForBuild;
            SelectBasePlaylist.Click += OnSelectBasePlaylist;
            ResetBasePlaylist.Click += OnResetBasePlaylist;

            CheckForUpdates();
        }

        private void UpdateTestsCount(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TestRunPassed)
            {
                UpdateTestsCountWithNumbers();
                SolutionTests.Visibility = Visibility.Hidden;
                TestRunTests.Visibility = Visibility.Hidden;
            }
            else
            {
                SolutionTests.Visibility = Visibility.Visible;
                TestRunTests.Visibility = Visibility.Visible;

                UpdateTestsCountWithNa();
            }
        }

        private void UpdateTestsCountWithNumbers()
        {
             var statistics = _tfsHelpers.GetTestRun(BuildNumber).Statistics;

            Passed.Content = $"Passed - {statistics.PassedTests}";
            Failed.Content = $"Failed - {statistics.FailedTests}";
            Inconclusive.Content = $"Inconclusive - {statistics.TotalTests - statistics.PassedTests - statistics.FailedTests}";
        }

        private void UpdateTestsCountWithNa()
        {
            if (File.Exists(SolutionTestsFile) && File.Exists(PassedTestsFile) && File.Exists(FailedTestsFile))
            {
                var allTestsCount = File.ReadAllLines(SolutionTestsFile).Length;
                var passedTestsCount = File.ReadAllLines(PassedTestsFile).Length;
                var failedTestsCount = File.ReadAllLines(FailedTestsFile).Length;

                Passed.Content = $"Passed - {passedTestsCount}";
                Failed.Content = $"Failed - {failedTestsCount}";
                Inconclusive.Content = $"Inconclusive - {allTestsCount - passedTestsCount - failedTestsCount}";
            }
            else
            {
                Passed.Content = "Passed - N/A";
                Failed.Content = "Failed - N/A";
                Inconclusive.Content = "Inconclusive - N/A";
            }
        }

        private void ReloadTestRuns(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                TestRuns.Items.Clear();
                _tfsHelpers.Builds.ForEach(build => TestRuns.Items.Add($"{build.Result} - {build.BuildNumber} - {build.FinishTime:D}"));

                TestRuns.SelectedIndex = 0;

                TestRuns.IsEnabled = true;
                Passed.IsEnabled = true;
                Failed.IsEnabled = true;
                Inconclusive.IsEnabled = true;
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to load test runs");
                Logger.Error(exception);

                AlertUser("Failed to load test runs", exception);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void AlertUser(string title, Exception exception)
        {
            MessageBox.Show(exception.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SavePlaylist(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (TestRunPassed)
            {
                SaveSucceededBuildPlaylist();
            }
            else
            {
                SaveFailedBuildPlaylist();
            }

            Mouse.OverrideCursor = null;
        }

        private void SaveFailedBuildPlaylist()
        {
            var tests = File.ReadAllLines(SolutionTestsFile);
            var passed = File.ReadAllLines(PassedTestsFile);
            var failed = File.ReadAllLines(FailedTestsFile);

            var content = string.Empty;

            if (PassedState)
            {
                foreach (var test in passed)
                {
                    var fullTestName = tests.First(x => x.EndsWith($".{test}"));
                    content += fullTestName.AsPlaylistEntry();
                }
            }

            if (FailedState)
            {
                foreach (var test in failed)
                {
                    var fullTestName = tests.First(x => x.EndsWith($".{test}"));
                    content += fullTestName.AsPlaylistEntry();
                }
            }

            if (InconclusiveState)
            {
                foreach (var test in tests)
                {
                    if (passed.Any(x => test.EndsWith($".{x}"))) continue;
                    if (failed.Any(x => test.EndsWith($".{x}"))) continue;

                    content += test.AsPlaylistEntry();
                }
            }

            var saveFileDialog = new SaveFileDialog { FileName = PlaylistName, Filter = "Playlist Files (*.playlist)|*.playlist" };
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName,
                    $@"<Playlist Version=""1.0"">{Environment.NewLine}{content}</Playlist>");
        }

        private void SaveSucceededBuildPlaylist()
        {
            try
            {
                var testRunId = _tfsHelpers.GetTestRun(BuildNumber).Id;
                var tests = _tfsHelpers.TestCaseResults(testRunId);
                var content = string.Empty;

                if (PassedState)
                    content += GetTestCasesByOutcome(tests, _passedOutcomes);

                if (FailedState)
                    content += GetTestCasesByOutcome(tests, _failedOutcomes);

                if (InconclusiveState)
                    content += GetTestCasesByOutcome(tests, _incompleteOutcomes);

                var saveFileDialog = new SaveFileDialog {FileName = PlaylistName, Filter = "Playlist Files (*.playlist)|*.playlist" };
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName,
                        $@"<Playlist Version=""1.0"">{Environment.NewLine}{content}</Playlist>");
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to save playlist");
                Logger.Error(exception);

                AlertUser("Failed to save playlist", exception);
            }
        }

        private string GetTestCasesByOutcome(List<ITestCaseResult> tests, params TestOutcome[] outcomes)
        {
            var result = string.Empty;
            var useBasePlaylist = !string.IsNullOrEmpty(BasePlaylistNameFile);
            var baseTests = useBasePlaylist ? File.ReadLines(BasePlaylistNameFile).ToList() : new List<string>();

            tests
                .Where(test => ListHelper<TestOutcome>.ExistsIn(test.Outcome, outcomes)).ToList()
                .Select(test => test.Implementation.DisplayText.AsPlaylistEntry(false))
                .Where(entry => !useBasePlaylist || baseTests.Any(x => x.Contains(entry))).ToList()
                .ForEach(entry => result += entry + "\r");

            return result;
        }

        private void GetTestsForBuild(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var logs = _tfsHelpers.GetBuild(BuildNumber).Logs.Url;

            if (!Directory.Exists(TestRunsFolder) || !Directory.GetFiles(TestRunsFolder).Any(file => file.Contains(BuildNumber)))
            {
                GetLogs(logs);
            }
            GetTestNames();

            Mouse.OverrideCursor = null;
        }

        private void OnResetBasePlaylist(object sender, RoutedEventArgs e)
        {
            BasePlaylistName.Visibility = Visibility.Hidden;
            BasePlaylistNameFile = string.Empty;
        }

        private void OnSelectBasePlaylist(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Playlist Files (*.playlist)|*.playlist" };
            if (openFileDialog.ShowDialog() ?? false)
            {
                BasePlaylistNameFile = openFileDialog.FileName;
                BasePlaylistName.Visibility = Visibility.Visible;
                BasePlaylistName.Content = BasePlaylistNameFile.Split("\\").Last();
            }
        }

        private void GetSolutionTests(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var solutionPath = ConfigurationManager.AppSettings["TestsSolutionPath"];
            var files = GetSharpFiles(solutionPath);
            var tests = new List<string>();

            files.ForEach(file => tests.AddRange(GetTests(file)));
            tests = tests.Distinct().ToList();

            File.Delete(SolutionTestsFile);
            File.WriteAllLines(SolutionTestsFile, tests);

            Mouse.OverrideCursor = null;
        }

        private List<string> GetTests(string file)
        {
            var result = new List<string>();
            var lines = File.ReadAllLines(file);

            if (lines.Any(line => line.Contains("[TestMethod]")))
            {
                bool testMethod = false;
                var nameSpace = lines.First(line => line.Contains("namespace")).Split("namespace").Last().Trim();

                foreach (var line in lines)
                {
                    if (line.Contains("[TestMethod]")) testMethod = true;

                    if (testMethod && line.Contains("public void "))
                    {
                        var test = line.SubString("public void", "()").Trim();
                        result.Add($"{nameSpace}.{test}");

                        testMethod = false;
                    }
                }
            }

            return result;
        }

        private List<string> GetSharpFiles(string folder)
        {
            var result = new List<string>();
            var files = Directory.GetFiles(folder);
            var subfolders = Directory.GetDirectories(folder);

            foreach (var file in files)
            {
                if (file.ToLower().EndsWith(".cs"))
                {
                    result.Add(file);
                }
            }

            foreach (var subfolder in subfolders)
            {
                result.AddRange(GetSharpFiles(subfolder));
            }

            return result;
        }

        private void GetLogs(string logs)
        {
            Directory.CreateDirectory(TestRunsFolder);
            Directory.GetFiles(TestRunsFolder).ToList()
                .Where(file => file.Contains(BuildNumber)).ToList()
                .ForEach(File.Delete);

            for (var i = 1; i < 100; i++)
            {
                var successful = HttpHelper.GetFile($"{logs}/{i}", $"{TestRunsFolder}\\{BuildNumber}_log_{i}.txt");

                if (!successful) break;
            }
        }

        private void GetTestNames()
        {
            const string regex = @"\d*-\d*-\d*T\d*:\d*:\d*.\d*Z\s*(Passed|Failed)";
            var tests = new List<string>();

            var files = Directory.GetFiles(TestRunsFolder);

            foreach (var file in files)
            {
                if (file.Contains(BuildNumber))
                {
                    var lines = File.ReadLines(file);
                    tests.AddRange(lines.Where(line => Regex.IsMatch(line, regex)));
                }
            }

            var passed = tests.Where(line => line.Contains(" Passed "))
                .Select(line => line.Split(" Passed ").Second().Trim()).Distinct();

            var failed = tests.Where(line => line.Contains(" Failed "))
                .Select(line => line.Split(" Failed ").Second().Trim()).Distinct();

            File.WriteAllLines(PassedTestsFile, passed);
            File.WriteAllLines(FailedTestsFile, failed);
            
            UpdateTestsCountWithNa();
        }

        private void CheckBoxClick(object sender, RoutedEventArgs e)
        {
            SaveBtn.IsEnabled = (PassedState || FailedState || InconclusiveState) &&
                                (TestRunPassed || File.Exists(SolutionTestsFile) &&
                                                  File.Exists(PassedTestsFile) &&
                                                  File.Exists(FailedTestsFile));
        }

        private static void CheckForUpdates()
        {
            var assemblyInfo = HttpHelper.GetFileContent(ConfigurationManager.AppSettings["AssemblyInfo"]);
            var availableVersion = assemblyInfo.FirstOrDefault(x => x.StartsWith("[assembly: AssemblyVersion")).SubString("\"", "\"");
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var question = $"Version {availableVersion} is available. Would you like download it?";
            const string caption = "New version is available";

            Logger.Info($"Current version: {currentVersion}; New version: {availableVersion}");

            if (currentVersion.IsVersionLessThen(availableVersion) &&
                MessageBox.Show(question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(ConfigurationManager.AppSettings["GitRepository"]);
            }
        }
    }
}