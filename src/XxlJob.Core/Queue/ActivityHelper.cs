using System.Diagnostics;
using System.Globalization;

namespace XxlJob.Core.Queue;

internal static class ActivityHelper
{
    public static ActivitySource XxlJobSource { get; }= new(
        "XxlJob",
        typeof(ActivityHelper).Assembly.GetName().Version.ToString());

    public static void RecordException(this Activity activity, Exception ex)
    {
        var tags = new ActivityTagsCollection
        {
            {
                "exception.type",
                ex.GetType().FullName
            },
            {
                "exception.stacktrace",
                ToInvariantString(ex)
            }
        };

        if (!string.IsNullOrWhiteSpace(ex.Message))
            tags.Add("exception.message", ex.Message);

        activity.SetStatus(ActivityStatusCode.Error, ex.Message);

        activity.AddEvent(new("exception", tags: tags));
    }

    private static string ToInvariantString(Exception exception)
    {
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }
}
