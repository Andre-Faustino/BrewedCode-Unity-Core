using System.Collections.Generic;
using UnityEngine;
using BrewedCode.Logging;

namespace BrewedCode.Theme
{
    public class ThemeValidationResult
    {
        public List<string> errors = new();
        public List<string> warnings = new();

        public bool IsValid => errors.Count == 0;

        private ILog? _logger;

        public void AddError(string msg) => errors.Add(msg);
        public void AddWarning(string msg) => warnings.Add(msg);

#if UNITY_EDITOR
        public void PrintToConsole()
        {
            InitializeLogger();

            if (IsValid)
            {
                _logger.InfoSafe("Validation SUCCESS.");
            }
            else
            {
                _logger.ErrorSafe("Validation FAILED.");
            }

            foreach (var w in warnings)
                _logger.WarningSafe(w);

            foreach (var e in errors)
                _logger.ErrorSafe(e);
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ThemeValidationResult));
            }
            catch
            {
                _logger = null;
            }
        }
#endif
    }
}