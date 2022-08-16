namespace LoadingScreenMod
{
    internal sealed class Triple
    {
        internal object obj;

        internal byte[] bytes;

        internal int code;

        internal Triple(object obj, byte[] bytes, int code)
        {
            this.obj = obj;
            this.bytes = bytes;
            this.code = code;
        }

        internal Triple(object obj, int code)
        {
            this.obj = obj;
            this.code = code;
        }

        internal Triple(int index)
        {
            code = index;
        }
    }
}
