using System;
using UnityEngine;

namespace LoadingScreenMod
{
    internal sealed class SimpleProfilerSource : ProfilerSource
    {
        private bool failed;

        internal SimpleProfilerSource(string name, LoadingProfiler profiler)
            : base(name, 1, profiler)
        {
        }

        protected internal override string CreateText()
        {
            try
            {
                if (failed)
                {
                    return sink.CreateText(isLoading: false, failed: true);
                }
                LoadingProfiler.Event[] buffer = events.m_buffer;
                for (int num = events.m_size - 1; num >= 0; num--)
                {
                    switch (buffer[num].m_type)
                    {
                        case LoadingProfiler.Type.BeginLoading:
                        case LoadingProfiler.Type.BeginSerialize:
                        case LoadingProfiler.Type.BeginDeserialize:
                        case LoadingProfiler.Type.BeginAfterDeserialize:
                            if (num != index || base.IsLoading)
                            {
                                index = num;
                                sink.Add(buffer[num].m_name);
                                return sink.CreateText(isLoading: true);
                            }
                            sink.Clear();
                            return sink.CreateText(isLoading: false);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            return null;
        }

        internal void Failed(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = sink.Last;
                if (string.IsNullOrEmpty(message))
                {
                    message = "Deserialize";
                }
            }
            else if (message.Length > 80)
            {
                message = message.Substring(0, 80);
            }
            if (!message.StartsWith("<color #f04040>"))
            {
                message = "<color #f04040>" + message + "</color>";
            }
            sink.Clear();
            sink.Add(message);
            failed = true;
        }
    }
}
