using System.Diagnostics;
using OpenTelemetry;

namespace LogBattery;

internal sealed class TraceStoreProcessor(TraceStore store) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        store.Add(data);
    }
}
