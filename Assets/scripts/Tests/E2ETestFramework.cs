using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace E2ETests
{
    public enum TestStatus
    {
        Passed,
        Failed,
        Pending,
        Skipped
    }

    [Serializable]
    public class TestCaseResult
    {
        public string TestId;
        public string FeatureId;
        public int Tier;
        public string Name;
        public string Description;
        public TestStatus Status;
        public string Message;
        public string StackTrace;
        public double DurationMs;

        public override string ToString()
        {
            return $"[{Status}] {TestId} ({FeatureId}, Tier {Tier}): {Name}" +
                   (string.IsNullOrEmpty(Message) ? "" : $" -> {Message}");
        }
    }

    [Serializable]
    public class TestSuiteReport
    {
        public string SuiteName = "E2E Test Suite - 2D Top-Down Shooter";
        public DateTime Timestamp = DateTime.UtcNow;
        public int TotalCount;
        public int PassedCount;
        public int FailedCount;
        public int PendingCount;
        public int SkippedCount;
        public double TotalDurationMs;
        public List<TestCaseResult> Results = new List<TestCaseResult>();

        public void AddResult(TestCaseResult result)
        {
            Results.Add(result);
            TotalCount++;
            switch (result.Status)
            {
                case TestStatus.Passed: PassedCount++; break;
                case TestStatus.Failed: FailedCount++; break;
                case TestStatus.Pending: PendingCount++; break;
                case TestStatus.Skipped: SkippedCount++; break;
            }
        }

        public string GenerateMarkdownSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# E2E Test Suite Execution Report");
            sb.AppendLine($"**Executed At**: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC  ");
            sb.AppendLine($"**Total Tests**: {TotalCount} | **Passed**: {PassedCount} | **Failed**: {FailedCount} | **Pending**: {PendingCount} | **Skipped**: {SkippedCount}  ");
            sb.AppendLine($"**Duration**: {TotalDurationMs:F2} ms  ");
            sb.AppendLine();

            sb.AppendLine("## Summary by Tier");
            for (int tier = 1; tier <= 4; tier++)
            {
                int tTotal = 0, tPass = 0, tFail = 0, tPend = 0;
                foreach (var r in Results)
                {
                    if (r.Tier == tier)
                    {
                        tTotal++;
                        if (r.Status == TestStatus.Passed) tPass++;
                        else if (r.Status == TestStatus.Failed) tFail++;
                        else if (r.Status == TestStatus.Pending) tPend++;
                    }
                }
                string tierTitle = tier switch
                {
                    1 => "Tier 1: Feature Coverage (Happy Path)",
                    2 => "Tier 2: Boundary & Corner Cases",
                    3 => "Tier 3: Pairwise Combinations",
                    4 => "Tier 4: Real-World Scenarios",
                    _ => $"Tier {tier}"
                };
                sb.AppendLine($"- **{tierTitle}**: {tPass}/{tTotal} Passed, {tFail} Failed, {tPend} Pending");
            }
            sb.AppendLine();

            if (FailedCount > 0)
            {
                sb.AppendLine("## Failed Tests");
                foreach (var r in Results)
                {
                    if (r.Status == TestStatus.Failed)
                    {
                        sb.AppendLine($"- ❌ **{r.TestId}** ({r.FeatureId}): {r.Name}");
                        sb.AppendLine($"  - *Error*: {r.Message}");
                    }
                }
                sb.AppendLine();
            }

            if (PendingCount > 0)
            {
                sb.AppendLine("## Pending Implementation (Progressive Verification)");
                sb.AppendLine($"*A total of {PendingCount} tests are currently pending implementation of later milestone components.*");
            }

            return sb.ToString();
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"suiteName\":\"{SuiteName}\",");
            sb.Append($"\"timestamp\":\"{Timestamp:o}\",");
            sb.Append($"\"totalCount\":{TotalCount},");
            sb.Append($"\"passedCount\":{PassedCount},");
            sb.Append($"\"failedCount\":{FailedCount},");
            sb.Append($"\"pendingCount\":{PendingCount},");
            sb.Append($"\"skippedCount\":{SkippedCount},");
            sb.Append($"\"totalDurationMs\":{TotalDurationMs:F2},");
            sb.Append("\"results\":[");
            for (int i = 0; i < Results.Count; i++)
            {
                var r = Results[i];
                if (i > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"testId\":\"{EscapeJson(r.TestId)}\",");
                sb.Append($"\"featureId\":\"{EscapeJson(r.FeatureId)}\",");
                sb.Append($"\"tier\":{r.Tier},");
                sb.Append($"\"name\":\"{EscapeJson(r.Name)}\",");
                sb.Append($"\"status\":\"{r.Status}\",");
                sb.Append($"\"message\":\"{EscapeJson(r.Message)}\",");
                sb.Append($"\"durationMs\":{r.DurationMs:F2}");
                sb.Append("}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    public class E2EAssertionException : Exception
    {
        public E2EAssertionException(string message) : base(message) { }
    }

    public class PendingException : Exception
    {
        public PendingException(string reason) : base(reason) { }
    }

    public static class E2EAssert
    {
        public static void IsTrue(bool condition, string message = "Expected true, got false")
        {
            if (!condition) throw new E2EAssertionException(message);
        }

        public static void IsFalse(bool condition, string message = "Expected false, got true")
        {
            if (condition) throw new E2EAssertionException(message);
        }

        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                string msg = message != null ? $"{message} - " : "";
                throw new E2EAssertionException($"{msg}Expected: <{expected}>, Actual: <{actual}>");
            }
        }

        public static void AreApproximatelyEqual(float expected, float actual, float tolerance = 0.001f, string message = null)
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                string msg = message != null ? $"{message} - " : "";
                throw new E2EAssertionException($"{msg}Expected approx: {expected} (+/- {tolerance}), Actual: {actual}");
            }
        }

        public static void AreApproximatelyEqual(Vector2 expected, Vector2 actual, float tolerance = 0.001f, string message = null)
        {
            if (Vector2.Distance(expected, actual) > tolerance)
            {
                string msg = message != null ? $"{message} - " : "";
                throw new E2EAssertionException($"{msg}Expected approx: {expected} (+/- {tolerance}), Actual: {actual}");
            }
        }

        public static void IsNotNull(object obj, string message = "Expected object not to be null")
        {
            if (obj == null) throw new E2EAssertionException(message);
        }

        public static void IsNull(object obj, string message = "Expected object to be null")
        {
            if (obj != null) throw new E2EAssertionException(message);
        }

        public static void Fail(string message = "Test failed")
        {
            throw new E2EAssertionException(message);
        }

        public static void Pending(string reason)
        {
            throw new PendingException(reason);
        }
    }

    public static class E2EReflector
    {
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        public static Type FindType(string typeName)
        {
            if (typeCache.TryGetValue(typeName, out var cached))
                return cached;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    typeCache[typeName] = type;
                    return type;
                }
            }
            return null;
        }

        public static bool HasType(string typeName)
        {
            return FindType(typeName) != null;
        }

        public static Component AddComponentByName(GameObject go, string typeName)
        {
            var type = FindType(typeName);
            if (type == null) return null;
            return go.AddComponent(type);
        }

        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return null;
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }

        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj == null) return;
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            prop?.SetValue(obj, value);
        }

        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null) return null;
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            if (obj == null) return;
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        public static object InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            if (obj == null) return null;
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) throw new MissingMethodException(obj.GetType().Name, methodName);
            return method.Invoke(obj, parameters);
        }
    }

    public static class TestRunnerHelper
    {
        public static void RunTest(TestSuiteReport report, string testId, string featureId, int tier, string name, string description, Action testAction)
        {
            var sw = Stopwatch.StartNew();
            var result = new TestCaseResult
            {
                TestId = testId,
                FeatureId = featureId,
                Tier = tier,
                Name = name,
                Description = description
            };
            try
            {
                testAction();
                result.Status = TestStatus.Passed;
            }
            catch (PendingException pex)
            {
                result.Status = TestStatus.Pending;
                result.Message = pex.Message;
            }
            catch (E2EAssertionException aex)
            {
                result.Status = TestStatus.Failed;
                result.Message = aex.Message;
                result.StackTrace = aex.StackTrace;
            }
            catch (Exception ex)
            {
                result.Status = TestStatus.Failed;
                result.Message = $"Unhandled Exception: {ex.GetType().Name}: {ex.Message}";
                result.StackTrace = ex.StackTrace;
            }
            finally
            {
                sw.Stop();
                result.DurationMs = sw.Elapsed.TotalMilliseconds;
                report.AddResult(result);
            }
        }
    }

    public class E2ETestContext : IDisposable
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private static int _liveDepth = 0;
        private static readonly System.Reflection.BindingFlags _priv = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        private readonly HashSet<int> _preexistingIds = new HashSet<int>();
        private readonly bool _isOutermost;
        private readonly float _entryTimeScale;
        // Scene-singleton isolation: detach scene subscribers from static events and
        // snapshot their fields so EditMode test runs can never contaminate the saved
        // scene (previously baked score 4820 + VictoryContinues into shooting.unity).
        private GameManager _sceneGm;
        private UIManager _sceneUi;
        private System.Action<int> _gmScoreHandler;
        private System.Action _gmDefeatedHandler;
        private System.Action<BossController> _uiSpawnedHandler;
        private System.Action<int, int> _uiHealthHandler;
        private System.Action _uiDefeatedHandler;
        private int _gmState, _gmScore, _gmNextThr, _gmLastThr, _gmInterval;
        private float _spSurvival;
        private bool _spBossSpawned, _spBossActive, _spSpawning;

        public E2ETestContext()
        {
            // These suites are EditMode-only: running inside PlayMode corrupts both
            // the live game (orphan sweep deletes live entities) and the results
            // (pause guards freeze logic, Destroy is delayed). Fail fast instead.
            if (Application.isPlaying)
                throw new System.InvalidOperationException(
                    "E2ETestContext requires EditMode. Exit PlayMode before running test suites.");
            // Snapshot global clock: many suites leave Time.timeScale at 0 (victory/
            // gameover/paused flows). Restoring on Dispose keeps every test hermetic
            // regardless of suite execution order (e.g. ThrowGrenade guards on ts>0).
            _entryTimeScale = Time.timeScale;
            _isOutermost = (_liveDepth++ == 0);
            if (_isOutermost)
            {
                // Enforce the documented test invariant ("Default timeScale must be
                // 1.0f"): every test starts with a running clock no matter which
                // suite ran before (victory/gameover flows leave ts=0 behind).
                // Tests needing a paused clock set it explicitly themselves.
                try { Time.timeScale = 1f; } catch { }
            }
            if (_isOutermost)
            {
                // Snapshot everything alive so Dispose can sweep untracked orphans
                // (e.g. Instantiate clones from RollGrenadeDrop/FireRadialBurst that
                // would otherwise linger in the scene in EditMode runs).
                try
                {
                    foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
                    {
                        if (go != null) _preexistingIds.Add(go.GetInstanceID());
                    }
                }
                catch { }
                DetachSceneSingletons();
            }
        }

        private static T GetPriv<T>(object obj, string field)
        {
            var f = obj.GetType().GetField(field, _priv);
            return (T)f.GetValue(obj);
        }

        private static void SetPriv(object obj, string field, object value)
        {
            var f = obj.GetType().GetField(field, _priv);
            f.SetValue(obj, value);
        }

        private static System.Delegate MakeHandler(System.Type delegateType, object target, string method)
        {
            var m = target.GetType().GetMethod(method, _priv);
            if (m == null) return null;
            return System.Delegate.CreateDelegate(delegateType, target, m);
        }

        /// <summary>
        /// Unplugs scene singletons from static gameplay events and snapshots their
        /// fields. Test bodies use their own instances; without this, static events
        /// (kill score, boss defeat) and direct FindObjectOfType calls mutate the
        /// saved scene objects during EditMode runs.
        /// </summary>
        private void DetachSceneSingletons()
        {
            try
            {
                _sceneGm = GameManager.Instance;
                _sceneUi = UIManager.Instance;
                if (_sceneGm != null)
                {
                    _gmState = (int)GetPriv<GameState>(_sceneGm, "_currentState");
                    _gmScore = GetPriv<int>(_sceneGm, "_currentScore");
                    _gmNextThr = GetPriv<int>(_sceneGm, "_nextBossScoreThreshold");
                    _gmLastThr = GetPriv<int>(_sceneGm, "_lastBossTriggerScore");
                    _gmInterval = GetPriv<int>(_sceneGm, "_bossScoreInterval");
                    _gmScoreHandler = (System.Action<int>)MakeHandler(typeof(System.Action<int>), _sceneGm, "HandleEnemyKilledScore");
                    if (_gmScoreHandler != null) EnemyBase.OnEnemyKilledScore -= _gmScoreHandler;
                    _gmDefeatedHandler = (System.Action)MakeHandler(typeof(System.Action), _sceneGm, "HandleBossDefeated");
                    if (_gmDefeatedHandler != null) BossController.OnBossDefeatedEvent -= _gmDefeatedHandler;
                }
                if (_sceneUi != null)
                {
                    _uiSpawnedHandler = (System.Action<BossController>)MakeHandler(typeof(System.Action<BossController>), _sceneUi, "HandleBossSpawned");
                    if (_uiSpawnedHandler != null) BossController.OnBossSpawned -= _uiSpawnedHandler;
                    _uiHealthHandler = (System.Action<int, int>)MakeHandler(typeof(System.Action<int, int>), _sceneUi, "HandleBossHealthChanged");
                    if (_uiHealthHandler != null) BossController.OnBossHealthChanged -= _uiHealthHandler;
                    _uiDefeatedHandler = (System.Action)MakeHandler(typeof(System.Action), _sceneUi, "HandleBossDefeated");
                    if (_uiDefeatedHandler != null) BossController.OnBossDefeatedEvent -= _uiDefeatedHandler;
                }
                var sp = UnityEngine.Object.FindObjectOfType<EnemySpawner>();
                if (sp != null)
                {
                    _spSurvival = sp.survivalTime;
                    _spBossSpawned = sp.bossSpawned;
                    _spBossActive = sp.isBossActive;
                    _spSpawning = sp.isSpawning;
                }
            }
            catch { }
        }

        private void ReattachSceneSingletons()
        {
            try
            {
                if (_sceneGm != null)
                {
                    if (_gmScoreHandler != null) EnemyBase.OnEnemyKilledScore += _gmScoreHandler;
                    if (_gmDefeatedHandler != null) BossController.OnBossDefeatedEvent += _gmDefeatedHandler;
                    SetPriv(_sceneGm, "_currentState", System.Enum.ToObject(typeof(GameState), _gmState));
                    SetPriv(_sceneGm, "_currentScore", _gmScore);
                    SetPriv(_sceneGm, "_nextBossScoreThreshold", _gmNextThr);
                    SetPriv(_sceneGm, "_lastBossTriggerScore", _gmLastThr);
                    SetPriv(_sceneGm, "_bossScoreInterval", _gmInterval);
                }
                if (_sceneUi != null)
                {
                    if (_uiSpawnedHandler != null) BossController.OnBossSpawned += _uiSpawnedHandler;
                    if (_uiHealthHandler != null) BossController.OnBossHealthChanged += _uiHealthHandler;
                    if (_uiDefeatedHandler != null) BossController.OnBossDefeatedEvent += _uiDefeatedHandler;
                }
                var sp = UnityEngine.Object.FindObjectOfType<EnemySpawner>();
                if (sp != null)
                {
                    sp.survivalTime = _spSurvival;
                    sp.bossSpawned = _spBossSpawned;
                    sp.isBossActive = _spBossActive;
                    sp.isSpawning = _spSpawning;
                }
            }
            catch { }
            _sceneGm = null;
            _sceneUi = null;
        }

        public GameObject CreateGameObject(string name = "E2E_Test_Object")
        {
            var go = new GameObject(name);
            createdObjects.Add(go);
            return go;
        }

        public GameObject CreateMockPlayer(Vector2 position, out Rigidbody2D rb)
        {
            var go = CreateGameObject("E2E_Mock_Player");
            go.transform.position = position;
            rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            go.tag = "Player";
            return go;
        }

        public GameObject CreateMockEnemy(string name, Vector2 position, int hp = 3)
        {
            var go = CreateGameObject($"E2E_Mock_{name}");
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            go.tag = "Enemy";
            return go;
        }

        public Camera CreateMockCamera()
        {
            var camGo = CreateGameObject("E2E_Mock_Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.transform.position = new Vector3(0, 0, -10);
            return cam;
        }

        public void StepPhysics(float dt = 0.02f)
        {
            var prevMode = Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                Physics2D.Simulate(dt);
            }
            finally
            {
                Physics2D.simulationMode = prevMode;
            }
        }

        public void Dispose()
        {
            foreach (var go in createdObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            createdObjects.Clear();
            // Outermost context restores scene-singleton wiring/fields first, then
            // sweeps untracked orphans created mid-test (Instantiate clones, stray
            // bullets/pickups). Zero impact on the test body itself since this runs
            // after assertions complete.
            if (_isOutermost)
            {
                ReattachSceneSingletons();
                try
                {
                    foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
                    {
                        if (go == null) continue;
                        if (_preexistingIds.Contains(go.GetInstanceID())) continue;
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }
                catch { }
                _preexistingIds.Clear();
            }
            // Restore global clock last so post-test state never leaks into the next test.
            try { Time.timeScale = _entryTimeScale; } catch { }
            _liveDepth = System.Math.Max(0, _liveDepth - 1);
        }
    }
}
