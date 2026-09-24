using NUnit.Framework;
using E2ETests;

namespace Tests.Editor
{
    [TestFixture]
    public class ScrollingMapEditModeTests
    {
        [Test]
        public void Tier1_FeatureCoverage_All50TestsPass()
        {
            var report = new TestSuiteReport();
            ScrollingMapTests.RunTier1(report);
            Assert.AreEqual(0, report.FailedCount, $"Tier 1 test failures:\n{report.GenerateMarkdownSummary()}");
            Assert.AreEqual(50, report.PassedCount, $"Expected 50 passed tests in Tier 1, got {report.PassedCount}");
        }

        [Test]
        public void Tier2_BoundariesAndCorners_All50TestsPass()
        {
            var report = new TestSuiteReport();
            ScrollingMapTests.RunTier2(report);
            Assert.AreEqual(0, report.FailedCount, $"Tier 2 test failures:\n{report.GenerateMarkdownSummary()}");
            Assert.AreEqual(50, report.PassedCount, $"Expected 50 passed tests in Tier 2, got {report.PassedCount}");
        }

        [Test]
        public void Tier3_CrossFeaturePairwise_All15TestsPass()
        {
            var report = new TestSuiteReport();
            ScrollingMapTests.RunTier3(report);
            Assert.AreEqual(0, report.FailedCount, $"Tier 3 test failures:\n{report.GenerateMarkdownSummary()}");
            Assert.AreEqual(15, report.PassedCount, $"Expected 15 passed tests in Tier 3, got {report.PassedCount}");
        }

        [Test]
        public void Tier4_RealWorldScenarios_All5TestsPass()
        {
            var report = new TestSuiteReport();
            ScrollingMapTests.RunTier4(report);
            Assert.AreEqual(0, report.FailedCount, $"Tier 4 test failures:\n{report.GenerateMarkdownSummary()}");
            Assert.AreEqual(5, report.PassedCount, $"Expected 5 passed tests in Tier 4, got {report.PassedCount}");
        }

        [Test]
        public void FullScrollingMapSuite_All120TestsPass()
        {
            var report = ScrollingMapTests.RunAll();
            Assert.AreEqual(0, report.FailedCount, $"Full suite test failures:\n{report.GenerateMarkdownSummary()}");
            Assert.AreEqual(120, report.PassedCount, $"Expected 120 passed tests, got {report.PassedCount}");
        }
    }
}
