using System;
using System.Diagnostics;
using UnityEngine;

namespace E2ETests
{
    public static class E2ETestRunner
    {
        public static TestSuiteReport RunAll()
        {
            var sw = Stopwatch.StartNew();
            var report = new TestSuiteReport();

            UnityEngine.Debug.Log("[E2ETestRunner] Starting E2E Test Suite Execution (Tiers 1-4)...");

            try
            {
                // Tier 1: Feature Coverage (Happy Path, F01-F35)
                E2ETier1Tests.RunAll(report);

                // Tier 2: Boundary & Corner Cases (F01-F35)
                E2ETier2Tests.RunAll(report);

                // Tier 3: Cross-Feature Pairwise Combinations
                E2ETier3Tests.RunAll(report);

                // Tier 4: Real-World Application Scenarios
                E2ETier4Tests.RunAll(report);

                // Endless Scrolling Map System Tests (Tiers 1-4, F01-F10)
                ScrollingMapTests.RunAll(report);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[E2ETestRunner] Fatal unhandled error in test suite: {ex}");
            }
            finally
            {
                sw.Stop();
                report.TotalDurationMs = sw.Elapsed.TotalMilliseconds;
            }

            UnityEngine.Debug.Log($"[E2ETestRunner] Execution Finished. Total: {report.TotalCount}, Passed: {report.PassedCount}, Failed: {report.FailedCount}, Pending: {report.PendingCount}, Skipped: {report.SkippedCount} in {report.TotalDurationMs:F2}ms.");
            return report;
        }

        public static string RunAllFormatted()
        {
            var report = RunAll();
            return report.GenerateMarkdownSummary();
        }

        public static string RunAllJson()
        {
            var report = RunAll();
            return report.ToJson();
        }

        public static TestSuiteReport RunTier(int tier)
        {
            var sw = Stopwatch.StartNew();
            var report = new TestSuiteReport();

            switch (tier)
            {
                case 1:
                    E2ETier1Tests.RunAll(report);
                    ScrollingMapTests.RunTier1(report);
                    break;
                case 2:
                    E2ETier2Tests.RunAll(report);
                    ScrollingMapTests.RunTier2(report);
                    break;
                case 3:
                    E2ETier3Tests.RunAll(report);
                    ScrollingMapTests.RunTier3(report);
                    break;
                case 4:
                    E2ETier4Tests.RunAll(report);
                    ScrollingMapTests.RunTier4(report);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), "Tier must be 1, 2, 3, or 4");
            }

            sw.Stop();
            report.TotalDurationMs = sw.Elapsed.TotalMilliseconds;
            return report;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("E2E Tests/Run All Tests")]
        public static void RunAllMenu()
        {
            string summary = RunAllFormatted();
            UnityEngine.Debug.Log(summary);
        }

        [UnityEditor.MenuItem("E2E Tests/Run Tier 1 (Coverage)")]
        public static void RunTier1Menu()
        {
            var report = RunTier(1);
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }

        [UnityEditor.MenuItem("E2E Tests/Run Tier 2 (Boundary)")]
        public static void RunTier2Menu()
        {
            var report = RunTier(2);
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }

        [UnityEditor.MenuItem("E2E Tests/Run Tier 3 (Pairwise)")]
        public static void RunTier3Menu()
        {
            var report = RunTier(3);
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }

        [UnityEditor.MenuItem("E2E Tests/Run Tier 4 (Scenarios)")]
        public static void RunTier4Menu()
        {
            var report = RunTier(4);
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }

        [UnityEditor.MenuItem("E2E Tests/Run Scrolling Map Tests (Tiers 1-4)")]
        public static void RunScrollingMapMenu()
        {
            var report = ScrollingMapTests.RunAll();
            UnityEngine.Debug.Log(report.GenerateMarkdownSummary());
        }
#endif
    }
}
