﻿using System.ComponentModel;
using System.Runtime.InteropServices;
using BeDbg.Api;

namespace BeDbg.Debugger;

/// <summary>
/// Debug a process created by CreateProcess
/// </summary>
public class CreateDebugger : BaseDebugger
{
	[DllImport(InteropConfig.Api64, EntryPoint = "StartProcess", CharSet = CharSet.Unicode)]
	private static extern int startProcess(string filename, string command, string? environment,
		string? workingDirectory);

	public CreateDebugger(string filename, string command, string? env, string? cwd)
	{
		DebugLoopThread = Task.Factory.StartNew(() =>
		{
			ApiError.Clear();
			var pid = startProcess(filename, command, env, cwd);
			if (pid == 0)
			{
				throw ApiError.FormatError();
			}


			var handle = BeDbg64.AttachProcess(pid).ToInt64();
			if (handle == 0)
			{
				throw ApiError.FormatError();
			}

			TargetPid = pid;
			TargetHandle = handle;

			Kernel.DebugActiveProcess(pid);
			StartDebugLoop();
			Kernel.DebugActiveProcessStop(pid);
		});
	}

	public override void OnRelease()
	{
		base.OnRelease();
		if (!Kernel.TerminateProcess(new IntPtr(TargetHandle), 0) && Kernel.GetLastError() != 0)
		{
			// throw new Win32Exception();
		}

		Kernel.CloseHandle(new IntPtr(TargetHandle));
	}
}