using System;
using System.Reflection;
using UnityEngine;

namespace LoadingScreenMod
{
    internal class ProfilerSource : Source
    {
        protected readonly Sink sink;

        protected readonly FastList<LoadingProfiler.Event> events;

        protected int index;

        private static FieldInfo EventsField => typeof(LoadingProfiler).GetField("m_events", BindingFlags.Instance | BindingFlags.NonPublic);

        protected bool IsLoading
        {
            get
            {
                int num = events.m_size - 1;
                LoadingProfiler.Event[] buffer = events.m_buffer;
                if (num >= 0)
                {
                    return (buffer[num].m_type & LoadingProfiler.Type.PauseLoading) == 0;
                }
                return false;
            }
        }

        internal static FastList<LoadingProfiler.Event> GetEvents(LoadingProfiler profiler)
        {
            return (FastList<LoadingProfiler.Event>)EventsField.GetValue(profiler);
        }

        internal ProfilerSource(string name, int len, LoadingProfiler profiler)
            : this(profiler, new Sink(name, len))
        {
        }

        internal ProfilerSource(LoadingProfiler profiler, Sink sink)
        {
            this.sink = sink;
            events = GetEvents(profiler);
        }

        protected internal override string CreateText()
        {
            try
            {
                int i = index;
                int size = events.m_size;
                if (i >= size)
                {
                    return null;
                }
                index = size;
                LoadingProfiler.Event[] buffer = events.m_buffer;
                for (; i < size; i++)
                {
                    switch (buffer[i].m_type)
                    {
                        case LoadingProfiler.Type.BeginLoading:
                        case LoadingProfiler.Type.BeginSerialize:
                        case LoadingProfiler.Type.BeginDeserialize:
                        case LoadingProfiler.Type.BeginAfterDeserialize:
                            sink.Add(buffer[i].m_name);
                            break;
                    }
                }
                return sink.CreateText(IsLoading);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }
    }
}
