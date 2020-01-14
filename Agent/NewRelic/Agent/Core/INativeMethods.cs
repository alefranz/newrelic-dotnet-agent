﻿using System;
using System.Runtime.InteropServices;

namespace NewRelic.Agent.Core
{
	public interface INativeMethods
	{
		void ReleaseProfile();
		int RequestFunctionNames(UIntPtr[] functionIds, int length, [Out] out IntPtr functionInfo);
		int RequestProfile([Out] out IntPtr snapshots, [Out] out int length);
		void ShutdownNativeThreadProfiler();

		int InstrumentationRefresh();
		int AddCustomInstrumentation(string fileName, string xml);
		int ApplyCustomInstrumentation();
	}
}
