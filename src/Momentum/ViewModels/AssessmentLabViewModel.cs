using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class AssessmentLabViewModel : PageViewModel
    {
        private readonly PythonExecutionHarness _harness = new();
        private CancellationTokenSource? _cts;

        private AssessmentQuestion? _selectedQuestion;
        private string _selectedTopicFilter = "All";
        private string _userCode = string.Empty;
        private bool _isRunning;
        private string _statusMessage = "Ready";
        private AssessmentRunReport? _lastReport;
        private FailureDiagnostic? _diagnostic;
        private bool _showHint;
        private bool _showReferenceSolution;
        private int _solvedCount;
        private int _attemptCount;
        private string _successRateText = "0%";
        private string _repeatedErrorSummary = "None logged";

        public AssessmentLabViewModel()
        {
            AllQuestions = AssessmentCatalog.Questions;
            Topics = new List<string> { "All" }
                .Concat(AllQuestions.Select(q => q.Topic).Distinct())
                .ToList();

            FilteredQuestions = new ObservableCollection<AssessmentQuestion>(AllQuestions);

            RunTestsCommand = new RelayCommand(async () => await RunAssessmentAsync(runAllTests: false), () => !IsRunning && SelectedQuestion != null);
            SubmitAssessmentCommand = new RelayCommand(async () => await RunAssessmentAsync(runAllTests: true), () => !IsRunning && SelectedQuestion != null);
            ResetStarterCodeCommand = new RelayCommand(ResetStarterCode, () => !IsRunning && SelectedQuestion != null);
            ToggleHintCommand = new RelayCommand(() => ShowHint = !ShowHint);
            ToggleReferenceCommand = new RelayCommand(() => ShowReferenceSolution = !ShowReferenceSolution);
            CopyCodeCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(UserCode))
                {
                    Clipboard.SetText(UserCode);
                    Shell.ShowToast("Code copied to clipboard.");
                }
            });

            // Select initial question
            var firstUnsolved = AllQuestions.FirstOrDefault(q => !Store.State.Lab.SolvedQuestionIds.Contains(q.Id))
                               ?? AllQuestions.FirstOrDefault();
            SelectQuestion(firstUnsolved);
            RefreshLabTelemetry();
        }

        public List<AssessmentQuestion> AllQuestions { get; }
        public List<string> Topics { get; }
        public ObservableCollection<AssessmentQuestion> FilteredQuestions { get; }
        public ObservableCollection<AssessmentTestResult> TestResults { get; } = new();

        public AssessmentQuestion? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                if (Set(ref _selectedQuestion, value))
                {
                    OnSelectedQuestionChanged();
                }
            }
        }

        public string SelectedTopicFilter
        {
            get => _selectedTopicFilter;
            set
            {
                if (Set(ref _selectedTopicFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string UserCode
        {
            get => _userCode;
            set
            {
                if (Set(ref _userCode, value))
                {
                    // Auto-save code for this question
                    if (SelectedQuestion != null)
                    {
                        Store.State.Lab.SavedCode[SelectedQuestion.Id] = value;
                        Store.ScheduleSave();
                    }
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (Set(ref _isRunning, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public AssessmentRunReport? LastReport
        {
            get => _lastReport;
            set
            {
                if (Set(ref _lastReport, value))
                {
                    OnPropertyChanged(nameof(IsSuccess));
                    OnPropertyChanged(nameof(IsFailure));
                }
            }
        }

        public bool IsSuccess => LastReport != null && LastReport.Success;
        public bool IsFailure => LastReport != null && !LastReport.Success;

        public FailureDiagnostic? Diagnostic
        {
            get => _diagnostic;
            set => Set(ref _diagnostic, value);
        }

        public bool ShowHint
        {
            get => _showHint;
            set => Set(ref _showHint, value);
        }

        public bool ShowReferenceSolution
        {
            get => _showReferenceSolution;
            set => Set(ref _showReferenceSolution, value);
        }

        public int SolvedCount
        {
            get => _solvedCount;
            set => Set(ref _solvedCount, value);
        }

        public int TotalCount => AllQuestions.Count;

        public int AttemptCount
        {
            get => _attemptCount;
            set => Set(ref _attemptCount, value);
        }

        public string SuccessRateText
        {
            get => _successRateText;
            set => Set(ref _successRateText, value);
        }

        public string RepeatedErrorSummary
        {
            get => _repeatedErrorSummary;
            set => Set(ref _repeatedErrorSummary, value);
        }

        public ICommand RunTestsCommand { get; }
        public ICommand SubmitAssessmentCommand { get; }
        public ICommand ResetStarterCodeCommand { get; }
        public ICommand ToggleHintCommand { get; }
        public ICommand ToggleReferenceCommand { get; }
        public ICommand CopyCodeCommand { get; }

        public override void OnShown()
        {
            base.OnShown();
            RefreshLabTelemetry();
        }

        public bool IsQuestionSolved(string questionId)
        {
            return Store.State.Lab.SolvedQuestionIds.Contains(questionId);
        }

        public void SelectQuestion(AssessmentQuestion? question)
        {
            SelectedQuestion = question;
        }

        private void OnSelectedQuestionChanged()
        {
            if (SelectedQuestion == null)
            {
                UserCode = string.Empty;
                TestResults.Clear();
                Diagnostic = null;
                LastReport = null;
                return;
            }

            // Restore saved user code or load starter code
            if (Store.State.Lab.SavedCode.TryGetValue(SelectedQuestion.Id, out var saved) && !string.IsNullOrWhiteSpace(saved))
            {
                UserCode = saved;
            }
            else
            {
                UserCode = SelectedQuestion.StarterCode;
            }

            TestResults.Clear();
            Diagnostic = null;
            LastReport = null;
            ShowHint = false;
            ShowReferenceSolution = false;
            StatusMessage = "Ready";

            // Pre-populate public test cases
            foreach (var tc in SelectedQuestion.TestCases.Where(t => !t.IsHidden))
            {
                TestResults.Add(new AssessmentTestResult
                {
                    Index = tc.Index,
                    Passed = false,
                    IsEvaluated = false,
                    IsHidden = false,
                    EdgeCaseTag = !string.IsNullOrWhiteSpace(tc.EdgeCaseTag) ? tc.EdgeCaseTag : null,
                    InputDisplay = tc.InputJson,
                    ExpectedDisplay = tc.ExpectedOutputJson,
                    ActualDisplay = string.Empty
                });
            }
        }

        private void ResetStarterCode()
        {
            if (SelectedQuestion == null) return;
            UserCode = SelectedQuestion.StarterCode;
            Store.State.Lab.SavedCode[SelectedQuestion.Id] = SelectedQuestion.StarterCode;
            Shell.ShowToast("Reset code to initial template.");
        }

        private void ApplyFilter()
        {
            FilteredQuestions.Clear();
            var matches = string.Equals(SelectedTopicFilter, "All", StringComparison.OrdinalIgnoreCase)
                ? AllQuestions
                : AllQuestions.Where(q => string.Equals(q.Topic, SelectedTopicFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var m in matches)
            {
                FilteredQuestions.Add(m);
            }

            if (SelectedQuestion != null && !FilteredQuestions.Contains(SelectedQuestion))
            {
                SelectQuestion(FilteredQuestions.FirstOrDefault());
            }
        }

        private async Task RunAssessmentAsync(bool runAllTests)
        {
            if (SelectedQuestion == null || IsRunning) return;

            IsRunning = true;
            StatusMessage = runAllTests ? "Submitting solution against full test suite..." : "Evaluating sample test cases...";
            Diagnostic = null;
            TestResults.Clear();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                // Prepare question slice if runAllTests is false (only public tests)
                var questionToRun = SelectedQuestion;
                if (!runAllTests)
                {
                    questionToRun = new AssessmentQuestion
                    {
                        Id = SelectedQuestion.Id,
                        Title = SelectedQuestion.Title,
                        FunctionName = SelectedQuestion.FunctionName,
                        ParameterNames = SelectedQuestion.ParameterNames,
                        TestCases = SelectedQuestion.TestCases.Where(t => !t.IsHidden).ToList()
                    };
                }

                var report = await _harness.ExecuteAssessmentAsync(questionToRun, UserCode, _cts.Token);
                LastReport = report;

                // Populate UI results
                foreach (var res in report.TestResults)
                {
                    TestResults.Add(res);
                }

                // Diagnose failure or victory
                var diag = FailureForensicsEngine.Diagnose(SelectedQuestion, UserCode, report);
                Diagnostic = diag;

                // Save attempt record
                var attempt = new AssessmentAttempt
                {
                    QuestionId = SelectedQuestion.Id,
                    Code = UserCode,
                    AllPassed = report.Success,
                    TestsPassed = report.TestResults.Count(t => t.Passed),
                    TotalTests = report.TestResults.Count,
                    ExecutionTimeMs = report.TotalExecutionMs,
                    FailureCategory = diag.Category,
                    DiagnosticSummary = diag.ObservedFact
                };

                Store.State.Lab.Attempts.Add(attempt);

                if (report.Success && runAllTests)
                {
                    Store.State.Lab.SolvedQuestionIds.Add(SelectedQuestion.Id);
                    Store.ScheduleSave();
                    StatusMessage = $"100% Passed ({report.TestResults.Count}/{report.TestResults.Count} tests) in {report.TotalExecutionMs}ms";
                    Shell.ShowToast($"Solved: {SelectedQuestion.Title}!");
                }
                else if (report.Success)
                {
                    StatusMessage = $"Sample Tests Passed ({report.TestResults.Count}/{report.TestResults.Count}). Submit to test hidden cases.";
                }
                else
                {
                    StatusMessage = $"Assessment Failed: {diag.CategoryDisplayName}";
                }

                Store.ScheduleSave();
                RefreshLabTelemetry();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Execution cancelled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Execution error: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void RefreshLabTelemetry()
        {
            var lab = Store.State.Lab;
            SolvedCount = lab.SolvedQuestionIds.Count;
            AttemptCount = lab.Attempts.Count;

            // Refresh solved flags on all catalog items for visual status indicators
            foreach (var q in AllQuestions)
            {
                q.IsSolved = lab.SolvedQuestionIds.Contains(q.Id);
            }

            if (AttemptCount > 0)
            {
                int successfulAttempts = lab.Attempts.Count(a => a.AllPassed);
                double rate = (double)successfulAttempts / AttemptCount * 100.0;
                SuccessRateText = $"{rate:0}% ({successfulAttempts}/{AttemptCount})";

                // Calculate most frequent error taxonomy
                var failures = lab.Attempts
                    .Where(a => !a.AllPassed && a.FailureCategory != FailureTaxonomy.None)
                    .GroupBy(a => a.FailureCategory)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (failures != null)
                {
                    RepeatedErrorSummary = $"{failures.Key} ({failures.Count()}x)";
                }
                else
                {
                    RepeatedErrorSummary = "None";
                }
            }
            else
            {
                SuccessRateText = "N/A";
                RepeatedErrorSummary = "None";
            }
        }
    }
}
