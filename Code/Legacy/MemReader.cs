using System;
using System.IO;
using System.Text;
using ColossalFramework.Packaging;

namespace LoadingScreenMod
{
	internal sealed class MemReader : PackageReader
	{
		private MemStream stream;

		private char[] charBuf = new char[96];

		private static readonly UTF8Encoding utf;

		static MemReader()
		{
			utf = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
		}

		protected override void Dispose(bool b)
		{
			stream = null;
			charBuf = null;
			base.Dispose(b);
		}

		public override float ReadSingle()
		{
			return stream.ReadSingle();
		}

		public override int ReadInt32()
		{
			return stream.ReadInt32();
		}

		public override byte ReadByte()
		{
			return stream.B8();
		}

		public override bool ReadBoolean()
		{
			return stream.B8() != 0;
		}

		internal MemReader(MemStream stream)
			: base(stream)
		{
			this.stream = stream;
		}

		public override ulong ReadUInt64()
		{
			int num = stream.ReadInt32();
			uint num2 = (uint)stream.ReadInt32();
			return (uint)num | ((ulong)num2 << 32);
		}

		public override string ReadString()
		{
			int num = ReadEncodedInt();
			if (num == 0)
			{
				return string.Empty;
			}
			if (num < 0 || num > 32767)
			{
				throw new IOException("Invalid string length " + num);
			}
			if (charBuf.Length < num)
			{
				charBuf = new char[num];
			}
			int chars = utf.GetChars(stream.Buf, stream.Pos, num, charBuf, 0);
			stream.Skip(num);
			return new string(charBuf, 0, chars);
		}

		private int ReadEncodedInt()
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < 5; i++)
			{
				byte b = stream.B8();
				num |= (b & 0x7F) << num2;
				num2 += 7;
				if ((b & 0x80) == 0)
				{
					return num;
				}
			}
			throw new FormatException("Too many bytes in what should have been a 7 bit encoded Int32.");
		}

		public override byte[] ReadBytes(int count)
		{
			byte[] array = new byte[count];
			Buffer.BlockCopy(stream.Buf, stream.Pos, array, 0, count);
			stream.Skip(count);
			return array;
		}

		//[HarmonyPatch(typeof(PackageReader), nameof(PackageReader.ReadByteArray))]
		//[HarmonyPrefix]
		private static bool DreadByteArray(ref byte[] __result, PackageReader __instance)
		{
			__result = __instance.ReadBytes(__instance.ReadInt32());
			return false;
		}
	}
}
