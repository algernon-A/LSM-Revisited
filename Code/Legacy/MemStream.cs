using System;
using System.IO;

namespace LoadingScreenMod
{
	internal sealed class MemStream : Stream
	{
		private byte[] buf;

		private int pos;

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Position
		{
			get
			{
				return pos;
			}
			set
			{
				pos = (int)value;
			}
		}

		public override long Length
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		internal byte[] Buf => buf;

		internal int Pos => pos;

		protected override void Dispose(bool b)
		{
			buf = null;
			base.Dispose(b);
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		internal MemStream(byte[] buf, int pos)
		{
			this.buf = buf;
			this.pos = pos;
		}

		internal byte B8()
		{
			return buf[pos++];
		}

		internal int I8()
		{
			return buf[pos++];
		}

		internal void Skip(int count)
		{
			pos += count;
		}

		internal int ReadInt32()
		{
			return I8() | (I8() << 8) | (I8() << 16) | (I8() << 24);
		}

		internal unsafe float ReadSingle()
		{
			float result = default(float);
			byte* ptr = (byte*)(&result);
			byte[] array = buf;
			*ptr = array[pos++];
			ptr[1] = array[pos++];
			ptr[2] = array[pos++];
			ptr[3] = array[pos++];
			return result;
		}

		public override int ReadByte()
		{
			return buf[pos++];
		}

		public override int Read(byte[] result, int offset, int count)
		{
			byte[] array = buf;
			for (int i = 0; i < count; i++)
			{
				result[offset++] = array[pos++];
			}
			return count;
		}
	}
}
