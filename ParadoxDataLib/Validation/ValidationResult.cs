using System.Collections.Generic;
using System.Linq;

namespace ParadoxDataLib.Validation
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public class ValidationIssue
    {
        public string PropertyName { get; set; }
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Context { get; set; }
        public int? LineNumber { get; set; }

        public ValidationIssue(string propertyName, string message, ValidationSeverity severity, string context = null, int? lineNumber = null)
        {
            PropertyName = propertyName;
            Message = message;
            Severity = severity;
            Context = context;
            LineNumber = lineNumber;
        }
    }

    public class ValidationResult
    {
        private readonly List<ValidationIssue> _issues;

        public ValidationResult()
        {
            _issues = new List<ValidationIssue>();
        }

        public IReadOnlyList<ValidationIssue> Issues => _issues;

        public bool IsValid => !_issues.Any(i => i.Severity >= ValidationSeverity.Error);

        public bool HasWarnings => _issues.Any(i => i.Severity == ValidationSeverity.Warning);

        public bool HasErrors => _issues.Any(i => i.Severity >= ValidationSeverity.Error);

        public bool HasCriticalErrors => _issues.Any(i => i.Severity == ValidationSeverity.Critical);

        public int ErrorCount => _issues.Count(i => i.Severity >= ValidationSeverity.Error);

        public int WarningCount => _issues.Count(i => i.Severity == ValidationSeverity.Warning);

        public void AddInfo(string propertyName, string message, string context = null, int? lineNumber = null)
        {
            _issues.Add(new ValidationIssue(propertyName, message, ValidationSeverity.Info, context, lineNumber));
        }

        public void AddWarning(string propertyName, string message, string context = null, int? lineNumber = null)
        {
            _issues.Add(new ValidationIssue(propertyName, message, ValidationSeverity.Warning, context, lineNumber));
        }

        public void AddError(string propertyName, string message, string context = null, int? lineNumber = null)
        {
            _issues.Add(new ValidationIssue(propertyName, message, ValidationSeverity.Error, context, lineNumber));
        }

        public void AddCriticalError(string propertyName, string message, string context = null, int? lineNumber = null)
        {
            _issues.Add(new ValidationIssue(propertyName, message, ValidationSeverity.Critical, context, lineNumber));
        }

        public void Merge(ValidationResult other)
        {
            if (other != null)
            {
                _issues.AddRange(other._issues);
            }
        }

        public ValidationResult GetIssuesBySeverity(ValidationSeverity severity)
        {
            var result = new ValidationResult();
            result._issues.AddRange(_issues.Where(i => i.Severity == severity));
            return result;
        }

        public ValidationResult GetIssuesForProperty(string propertyName)
        {
            var result = new ValidationResult();
            result._issues.AddRange(_issues.Where(i => i.PropertyName == propertyName));
            return result;
        }

        public override string ToString()
        {
            if (IsValid && !HasWarnings)
                return "Validation passed with no issues.";

            var lines = new List<string>();

            if (HasCriticalErrors)
                lines.Add($"Critical Errors: {_issues.Count(i => i.Severity == ValidationSeverity.Critical)}");
            if (HasErrors)
                lines.Add($"Errors: {_issues.Count(i => i.Severity == ValidationSeverity.Error)}");
            if (HasWarnings)
                lines.Add($"Warnings: {WarningCount}");

            foreach (var issue in _issues.OrderByDescending(i => i.Severity))
            {
                var prefix = issue.Severity switch
                {
                    ValidationSeverity.Critical => "[CRITICAL]",
                    ValidationSeverity.Error => "[ERROR]",
                    ValidationSeverity.Warning => "[WARN]",
                    ValidationSeverity.Info => "[INFO]",
                    _ => "[UNKNOWN]"
                };

                var line = $"{prefix} {issue.PropertyName}: {issue.Message}";

                if (issue.LineNumber.HasValue)
                    line += $" (Line {issue.LineNumber})";

                if (!string.IsNullOrEmpty(issue.Context))
                    line += $" [Context: {issue.Context}]";

                lines.Add(line);
            }

            return string.Join("\n", lines);
        }
    }
}