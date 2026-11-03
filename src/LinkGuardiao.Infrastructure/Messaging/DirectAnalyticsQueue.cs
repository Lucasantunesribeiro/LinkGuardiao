using LinkGuardiao.Application.Entities;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Telemetry;

namespace LinkGuardiao.Infrastructure.Messaging
{
    /// <summary>
    /// Grava o clique direto no repositorio, sem fila. Usado com STORAGE_PROVIDER=postgresql quando
    /// SQS_ANALYTICS_QUEUE_URL nao esta definido (ex.: demo gratuita sem AWS). Mesma logica do
    /// AnalyticsConsumer: deduplica pelo id do evento e so incrementa o contador se o registro for novo.
    /// O RedirectController ja trata excecoes daqui sem impedir o redirect.
    /// </summary>
    public sealed class DirectAnalyticsQueue : IAnalyticsQueue
    {
        private readonly IAccessLogRepository _accessLogs;
        private readonly ILinkRepository _links;

        public DirectAnalyticsQueue(IAccessLogRepository accessLogs, ILinkRepository links)
        {
            _accessLogs = accessLogs;
            _links = links;
        }

        public async Task EnqueueAsync(AccessLogMessage message, CancellationToken ct = default)
        {
            var access = new LinkAccess
            {
                Id = message.Id,
                ShortCode = message.ShortCode,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent,
                ReferrerUrl = message.ReferrerUrl,
                Browser = message.Browser,
                OperatingSystem = message.OperatingSystem,
                DeviceType = message.DeviceType,
                AccessTime = message.AccessTime
            };

            if (await _accessLogs.TryRecordAccessAsync(access, ct))
            {
                await _links.IncrementClickCountAsync(message.ShortCode, ct);
            }
            else
            {
                LinkGuardiaoMetrics.RecordAnalyticsEventDeduplicated();
            }
        }
    }
}
