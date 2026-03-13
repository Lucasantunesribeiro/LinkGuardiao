using System.Diagnostics.Metrics;

namespace LinkGuardiao.Application.Telemetry
{
    public static class LinkGuardiaoMetrics
    {
        private static readonly Meter Meter = new("LinkGuardiao.Application", "1.0.0");
        private static readonly Counter<long> LinksCreated = Meter.CreateCounter<long>("linkguardiao.links.created");
        private static readonly Counter<long> RedirectsServed = Meter.CreateCounter<long>("linkguardiao.redirects.served");
        private static readonly Counter<long> RedirectsBlocked = Meter.CreateCounter<long>("linkguardiao.redirects.blocked");
        private static readonly Counter<long> AccessGrantsIssued = Meter.CreateCounter<long>("linkguardiao.access_grants.issued");
        private static readonly Counter<long> AnalyticsMessagesEnqueued = Meter.CreateCounter<long>("linkguardiao.analytics.enqueued");
        private static readonly Counter<long> AnalyticsEnqueueFailures = Meter.CreateCounter<long>("linkguardiao.analytics.enqueue_failures");
        private static readonly Counter<long> AnalyticsEventsDeduplicated = Meter.CreateCounter<long>("linkguardiao.analytics.deduplicated");

        public static void RecordLinkCreated() => LinksCreated.Add(1);

        public static void RecordRedirectServed(bool passwordProtected) => RedirectsServed.Add(
            1,
            new KeyValuePair<string, object?>("password_protected", passwordProtected));

        public static void RecordRedirectBlocked(string reason) => RedirectsBlocked.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason));

        public static void RecordAccessGrantIssued() => AccessGrantsIssued.Add(1);

        public static void RecordAnalyticsMessageEnqueued() => AnalyticsMessagesEnqueued.Add(1);

        public static void RecordAnalyticsEnqueueFailure() => AnalyticsEnqueueFailures.Add(1);

        public static void RecordAnalyticsEventDeduplicated() => AnalyticsEventsDeduplicated.Add(1);
    }
}
