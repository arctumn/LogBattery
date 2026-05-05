using System.Diagnostics;
using OpenTelemetry;

namespace Arctumn.LogBattery.Tracing;

internal sealed class TraceStoreProcessor(TraceStore store) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        store.Add(data);
    }
}
