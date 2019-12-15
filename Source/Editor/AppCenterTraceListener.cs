using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer
{
    public class AppCenterTraceListener : TraceListener
    {
        public AppCenterTraceListener(string secret)
        {
            try
            {
                AppCenter.SetCountryCode(GeoRegionHelper.GetCountryCode());
                AppCenter.Start(
                    secret,
                    typeof(Analytics), typeof(Crashes));
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        public static void Initialize(string secret)
            => Trace.Listeners.Add(new AppCenterTraceListener(secret));

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void Write(object o)
        {
            base.Write(o);

            switch (o)
            {
                case Exception exception:
                    Crashes.TrackError(exception);
                    break;

                case AnalyticsEvent analyticsEvent:
                    Analytics.TrackEvent(
                        analyticsEvent.EventName,
                        analyticsEvent.Properties);
                    break;
            }
        }
    }

    public class AnalyticsEvent
    {
        public AnalyticsEvent(string eventName, Dictionary<string, string> properties = null)
        {
            EventName = eventName;
            Properties = properties;
        }

        public string EventName { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public override string ToString()
        {
            return EventName ?? base.ToString();
        }
    }
}
