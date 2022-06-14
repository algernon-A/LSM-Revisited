using System;
using System.Reflection;

namespace LoadingScreenMod
{
	public static class RedirectionHelper
	{
		public static RedirectCallsState RedirectCalls(MethodInfo from, MethodInfo to)
		{
			IntPtr functionPointer = from.MethodHandle.GetFunctionPointer();
			IntPtr functionPointer2 = to.MethodHandle.GetFunctionPointer();
			return PatchJumpTo(functionPointer, functionPointer2);
		}

		public static void RevertRedirect(MethodInfo m, RedirectCallsState state)
		{
			RevertJumpTo(m.MethodHandle.GetFunctionPointer(), state);
		}

		private unsafe static RedirectCallsState PatchJumpTo(IntPtr site, IntPtr target)
		{
			RedirectCallsState result = default(RedirectCallsState);
			byte* ptr = (byte*)site.ToPointer();
			result.a = *ptr;
			result.b = ptr[1];
			result.c = ptr[10];
			result.d = ptr[11];
			result.e = ptr[12];
			result.f = *(ulong*)(ptr + 2);
			*ptr = 73;
			ptr[1] = 187;
			*(long*)(ptr + 2) = target.ToInt64();
			ptr[10] = 65;
			ptr[11] = byte.MaxValue;
			ptr[12] = 227;
			return result;
		}

		private unsafe static void RevertJumpTo(IntPtr site, RedirectCallsState state)
		{
			byte* ptr = (byte*)site.ToPointer();
			*ptr = state.a;
			ptr[1] = state.b;
			*(ulong*)(ptr + 2) = state.f;
			ptr[10] = state.c;
			ptr[11] = state.d;
			ptr[12] = state.e;
		}
	}
}
