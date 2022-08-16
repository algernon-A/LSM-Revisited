using System.Collections.Generic;
using System.Text;

namespace LoadingScreenMod
{
    internal sealed class Sink
    {
        private string name;

        private string last;

        private readonly Queue<string> queue = new Queue<string>();

        private readonly int len;

        internal const string YELLOW = "<color #f0f000>";

        internal const string RED = "<color #f04040>";

        internal const string GRAY = "<color #c0c0c0>";

        internal const string ORANGE = "<color #f0a840>";

        internal const string CYAN = "<color #80e0f0>";

        internal const string OFF = "</color>";

        internal static readonly StringBuilder builder = new StringBuilder();

        internal string Name
        {
            set
            {
                name = value;
            }
        }

        internal string Last => last;

        private string NameLoading => "<color #f0f000>" + name + "</color>";

        private string NameIdle => "<color #c0c0c0>" + name + "</color>";

        private string NameFailed => "<color #f04040>" + name + Profiling.FAILED + "</color>";

        internal Sink(string name, int len)
        {
            this.name = name;
            this.len = len;
        }

        internal void Clear()
        {
            queue.Clear();
            last = null;
        }

        internal void Add(string s)
        {
            if (!(s != last))
            {
                return;
            }
            if (last != null && len > 1)
            {
                if (queue.Count >= len - 1)
                {
                    queue.Dequeue();
                }
                queue.Enqueue(last);
            }
            if (s[s.Length - 1] == ')')
            {
                if (s.EndsWith(Profiling.MISSING))
                {
                    s = "<color #f0a840>" + s + "</color>";
                }
                else if (s.EndsWith(Profiling.FAILED))
                {
                    s = "<color #f04040>" + s + "</color>";
                }
                else if (s.EndsWith(Profiling.DUPLICATE))
                {
                    s = "<color #80e0f0>" + s + "</color>";
                }
            }
            last = s;
        }

        internal string CreateText(bool isLoading, bool failed = false)
        {
            builder.AppendLine(isLoading ? NameLoading : (failed ? NameFailed : NameIdle));
            foreach (string item in queue)
            {
                builder.AppendLine(item);
            }
            if (last != null)
            {
                builder.Append(last);
            }
            string result = builder.ToString();
            builder.Length = 0;
            return result;
        }
    }
}
