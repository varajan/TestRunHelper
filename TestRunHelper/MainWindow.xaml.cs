using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private bool PassedState => Passed.IsChecked.HasValue && Passed.IsChecked.Value;
        private bool FailedState => Failed.IsChecked.HasValue && Failed.IsChecked.Value;
        private bool NotExecutedState => NotExecuted.IsChecked.HasValue && NotExecuted.IsChecked.Value;
        private bool InconclusiveState => Inconclusive.IsChecked.HasValue && Inconclusive.IsChecked.Value;

        private bool TestRunPassed => $"{TestRuns.SelectedItem}".Split('-').First().Contains("Succeeded");
        private string BuildNumber => $"{TestRuns.SelectedItem}".Split('-').Second().Trim();

        private readonly TfsHelper _tfsHelpers;
        
        public MainWindow()
        {
            InitializeComponent();

            _tfsHelpers = new TfsHelper();

            TestRuns.IsEnabled = false;
            Passed.IsEnabled = false;
            Failed.IsEnabled = false;
            NotExecuted.IsEnabled = false;
            Inconclusive.IsEnabled = false;
            SaveBtn.IsEnabled = false;

            Passed.Click += CheckBoxClick;
            Failed.Click += CheckBoxClick;
            NotExecuted.Click += CheckBoxClick;
            Inconclusive.Click += CheckBoxClick;
            SaveBtn.Click += SavePlaylist;
            Reload.Click += ReloadTestRuns;
            TestRuns.SelectionChanged += UpdateTestsCount;
        }

        private void UpdateTestsCount(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TestRunPassed) UpdateTestsCountWithNumbers(); else UpdateTestsCountWithNa();
        }

        private void UpdateTestsCountWithNumbers()
        {
             var statistics = _tfsHelpers.GetTestRun(BuildNumber).Statistics;

            Passed.Content = $"Passed - {statistics.PassedTests}";
            Failed.Content = $"Failed - {statistics.FailedTests}";
            NotExecuted.Content = "Not executed - 0";
            Inconclusive.Content = $"Inconclusive - {statistics.TotalTests - statistics.PassedTests - statistics.FailedTests}";
        }

        private void UpdateTestsCountWithNa()
        {
            Passed.Content = "Passed - N/A";
            Failed.Content = "Failed - N/A";
            NotExecuted.Content = "Not executed - N/A";
            Inconclusive.Content = "Inconclusive - N/A";
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
                NotExecuted.IsEnabled = true;
                Inconclusive.IsEnabled = true;
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to load test runs");
                Logger.Error(exception);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SavePlaylist(object sender, RoutedEventArgs e)
        {
            var passedOutcomes = new[]
            {
                TestOutcome.Passed
            };
            var failedOutcomes = new[]
            {
                TestOutcome.Failed,
                TestOutcome.Aborted,
                TestOutcome.Error,
                TestOutcome.Timeout,
                TestOutcome.MaxValue
            };
            var incompleteOutcomes = new[]
            {
                TestOutcome.Inconclusive,
                TestOutcome.NotApplicable,
                TestOutcome.InProgress,
                TestOutcome.Blocked,
                TestOutcome.Warning,
                TestOutcome.None
            };
            var notExecutedOutcomes = new[]
            {
                TestOutcome.NotExecuted
            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var TestRunId = 1;
                var tests = _tfsHelpers.TestCaseResults(TestRunId);
                var content = string.Empty;

                if (PassedState)
                    content += GetTestCasesByOutcome(tests, passedOutcomes);

                if (FailedState)
                    content += GetTestCasesByOutcome(tests, failedOutcomes);

                if (NotExecutedState)
                    content += GetTestCasesByOutcome(tests, notExecutedOutcomes);

                if (InconclusiveState)
                    content += GetTestCasesByOutcome(tests, incompleteOutcomes);

                var saveFileDialog = new SaveFileDialog {FileName = BuildNumber, Filter = "Playlist Files (*.playlist)|*.playlist" };
                if (saveFileDialog.ShowDialog() == true)
                    File.WriteAllText(saveFileDialog.FileName,
                        $@"<Playlist Version=""1.0"">{Environment.NewLine}{content}</Playlist>");
            }
            catch (Exception exception)
            {
                Logger.Error("Failed to save playlist");
                Logger.Error(exception);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private string GetTestCasesByOutcome(List<ITestCaseResult> tests, params TestOutcome[] outcomes)
        {
            var result = string.Empty;

            tests
                .Where(test => ListHelper<TestOutcome>.ExistsIn(test.Outcome, outcomes)).ToList()
                .ForEach(test => result += $"    <Add Test=\"{test.Implementation.DisplayText}\" />\r");

            return result;
        }

        private void CheckBoxClick(object sender, RoutedEventArgs e)
        {
            SaveBtn.IsEnabled = PassedState || FailedState || NotExecutedState || InconclusiveState;
        }
    }
}