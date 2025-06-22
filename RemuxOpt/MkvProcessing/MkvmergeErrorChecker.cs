using System.Text.RegularExpressions;

namespace RemuxOpt
{
    public static class MkvmergeErrorChecker
    {
        private static readonly Regex[] ErrorPatterns = {
            new Regex(@"Error:", RegexOptions.IgnoreCase),
            new Regex(@"Fatal error:", RegexOptions.IgnoreCase),
            new Regex(@"could not open", RegexOptions.IgnoreCase),
            new Regex(@"does not exist", RegexOptions.IgnoreCase),
            new Regex(@"invalid", RegexOptions.IgnoreCase),
            new Regex(@"failed", RegexOptions.IgnoreCase),
            new Regex(@"not supported", RegexOptions.IgnoreCase),
            new Regex(@"corrupted", RegexOptions.IgnoreCase),
            new Regex(@"cannot", RegexOptions.IgnoreCase),
            new Regex(@"unable to", RegexOptions.IgnoreCase)
        };

        private static readonly Regex[] WarningPatterns = {
            new Regex(@"Warning:", RegexOptions.IgnoreCase),
            new Regex(@"skipping", RegexOptions.IgnoreCase),
            new Regex(@"ignoring", RegexOptions.IgnoreCase),
            new Regex(@"deprecated", RegexOptions.IgnoreCase)
        };

        private static readonly Regex[] SuccessPatterns = {
            new Regex(@"Multiplexing took", RegexOptions.IgnoreCase),
            new Regex(@"Progress: 100%", RegexOptions.IgnoreCase),
            new Regex(@"The file has been saved", RegexOptions.IgnoreCase)
        };

        public static MkvmergeResult CheckForErrors(string outputText)
        {
            var result = new MkvmergeResult();

            if (string.IsNullOrEmpty(outputText))
            {
                result.ResultType = "No Output";
                return result;
            }

            // Parse output text for specific error/warning messages
            var lines = outputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                // Check for errors
                if (ErrorPatterns.Any(pattern => pattern.IsMatch(trimmedLine)))
                {
                    result.HasErrors = true;
                    result.Errors.Add(trimmedLine);
                    if (result.ResultType == "Success")
                    {
                        result.ResultType = "Error";
                    }
                }
                // Check for warnings (only if not already an error line)
                else if (WarningPatterns.Any(pattern => pattern.IsMatch(trimmedLine)))
                {
                    result.HasWarnings = true;
                    result.Warnings.Add(trimmedLine);
                    if (result.ResultType == "Success")
                    {
                        result.ResultType = "Warning";
                    }
                }
            }

            // Double-check success indicators if no errors found
            if (!result.HasErrors)
            {
                var hasSuccessIndicator = lines.Any(line => 
                    SuccessPatterns.Any(pattern => pattern.IsMatch(line)));
            
                if (hasSuccessIndicator && !result.HasWarnings)
                {
                    result.ResultType = "Success";
                }
                else if (hasSuccessIndicator && result.HasWarnings)
                {
                    result.ResultType = "Warning";
                }
                else if (!result.HasWarnings)
                {
                    result.ResultType = "Unknown";
                }
            }
            else
            {
                result.ResultType = "Error";
            }

            return result;
        }

        public static string GetSummary(MkvmergeResult result)
        {
            var summary = $"Result: {result.ResultType}";
        
            if (result.HasErrors)
            {
                summary += $"\nErrors ({result.Errors.Count}):";
                foreach (var error in result.Errors)
                {
                    summary += $"\n  • {error}";
                }
            }

            if (result.HasWarnings)
            {
                summary += $"\nWarnings ({result.Warnings.Count}):";
                foreach (var warning in result.Warnings)
                {
                    summary += $"\n  • {warning}";
                }
            }

            return summary;
        }
    }
}