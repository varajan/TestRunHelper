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
    public partial class MainWindow : Window
    {
        private readonly TfsTestRun _tfsTestRuns;
        public MainWindow()
        {
            InitializeComponent();

            _tfsTestRuns = new TfsTestRun();

            TestRuns.IsEnabled = false;
            Passed.IsEnabled = false;
            Failed.IsEnabled = false;
            Inconclusive.IsEnabled = false;

            SaveBtn.IsEnabled = false;
            SaveBtn.Click += SavePlaylist;

            Reload.Click += ReloadTestRuns;
        }

        private void ReloadTestRuns(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                TestRuns.Items.Clear();
                _tfsTestRuns.TestRuns.OrderByDescending(run => run.Id)
                    .ToList()
                    .ForEach(run => TestRuns.Items
                        .Add($"{run.Id} - {run.BuildNumber} - {run.DateCompleted:D}"));

                TestRuns.SelectedIndex = 0;

                TestRuns.IsEnabled = true;
                SaveBtn.IsEnabled = true;
                Passed.IsEnabled = true;
                Failed.IsEnabled = true;
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
            var passed = Passed.IsChecked.HasValue && Passed.IsChecked.Value;
            var failed = Failed.IsChecked.HasValue && Failed.IsChecked.Value;
            var inconclusive = Inconclusive.IsChecked.HasValue && Inconclusive.IsChecked.Value;
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
                TestOutcome.NotExecuted,
                TestOutcome.InProgress,
                TestOutcome.Blocked,
                TestOutcome.Warning,
                TestOutcome.None
            };

            try
            {
                if (passed || failed || inconclusive)
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    var tests = _tfsTestRuns.TestCaseResults($"{TestRuns.SelectedItem}".Split(' ').First().ToInt());
                    var content = string.Empty;

                    if (passed)
                        content += GetTestCasesByOutcome(tests, passedOutcomes);

                    if (failed)
                        content += GetTestCasesByOutcome(tests, failedOutcomes);

                    if (inconclusive)
                        content += GetTestCasesByOutcome(tests, incompleteOutcomes);

                    var saveFileDialog = new SaveFileDialog { Filter = "Playlist Files (*.playlist)|*.playlist" };
                    if (saveFileDialog.ShowDialog() == true)
                        File.WriteAllText(saveFileDialog.FileName, $@"<Playlist Version=""1.0"">\r{content}</Playlist>");
                }
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
                .ForEach(test => result += $"<Add Test=\"{test.Implementation.DisplayText}\" />\r");

            return result;
        }
    }
}