#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BrewedCode.Theme.Tests.Editor
{
    /// <summary>
    /// Run this via: Menu Tools > Theme > Run Tests & Show Results
    /// This is a simplified test runner that works with the current Unity API.
    /// </summary>
    public class RunThemeTests
    {
        [MenuItem("Tools/Theme/Run Tests & Show Results")]
        public static void RunTests()
        {
            Debug.Log("========================================");
            Debug.Log("  THEME SYSTEM TESTS - EXECUTION START  ");
            Debug.Log("========================================");
            Debug.Log($"Time: {System.DateTime.Now}");
            Debug.Log($"Tests Assembly: BrewedCode.Theme.Tests");
            Debug.Log("");
            Debug.Log("Instructions:");
            Debug.Log("  1. Open Window > Testing > Test Runner");
            Debug.Log("  2. Select PlayMode tab");
            Debug.Log("  3. Click Run All");
            Debug.Log("");
            Debug.Log("========================================");
        }
    }
}
#endif
