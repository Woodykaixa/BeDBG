﻿using System.Runtime.InteropServices;
using BeDbg.Api;
using BeDbg.Util;

namespace BeDbg.Debugger;

public enum DebugContinueStatus : byte
{
	AutoContinue = 0,
	NotHandled = 1,
	WaitForExplicitContinue = 2
}

/// <summary>
/// <para>
///	Handle <a href="https://docs.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-debug_event">DEBUG_EVENT</a>.
/// Each abstract OnXxx method handles one debug event. When <see cref="DebugLoopWaitEvent">DebugLoopWaitEvent</see> is called
/// in DebugLoop, it retrieves <i>DEBUG_EVENT</i>, and dispatch to corresponding handler depends on <i>dwDebugEventCode</i>.
/// </para>
///
/// <para>
/// For more information about debug event's fields,
/// see https://docs.microsoft.com/en-us/windows/win32/debug/debugging-events
/// </para>
/// </summary>
public abstract class DebugEventHandler : NeedRelease
{
	/// <summary>
	/// Handles EXCEPTION_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <b>AutoContinue</b> represents DBG_CONTINUE, <b>NotHandled</b> represents DBG_EXCEPTION_NOT_HANDLED,
	/// <b>WaitForExplicitContinue</b> represents you should call DebugContinue
	/// </returns>
	public abstract unsafe DebugContinueStatus OnException(uint process, uint thread, void* info);

	/// <summary>
	/// Handles CREATE_THREAD_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnCreateThread(uint process, uint thread, void* info);

	/// <summary>
	/// Handles CREATE_PROCESS_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnCreateProcess(uint process, uint thread, void* info);

	/// <summary>
	/// Handles EXIT_THREAD_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnExitThread(uint process, uint thread, void* info);

	/// <summary>
	/// Handles EXIT_PROCESS_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnExitProcess(uint process, uint thread, void* info);

	/// <summary>
	/// Handles LOAD_DLL_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnLoadDll(uint process, uint thread, void* info);

	/// <summary>
	/// Handles UNLOAD_DLL_DEBUG_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnUnloadDll(uint process, uint thread, void* info);

	/// <summary>
	/// Handles OUTPUT_DEBUG_STRING_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnOutputDebugString(uint process, uint thread, void* info);

	/// <summary>
	/// Handles RIP_EVENT
	/// </summary>
	/// <param name="process">Process id comes from <i>DEBUG_EVENT::dwProcessId</i></param>
	/// <param name="thread">Thread id comes from <i>DEBUG_EVENT::dwThreadId</i></param>
	/// <param name="info">XXX_DEBUG_INFO struct comes from <i>DEBUG_EVENT::u</i></param>
	/// <returns>
	/// <inheritdoc cref="OnException"/>
	/// </returns>
	public abstract unsafe DebugContinueStatus OnRip(uint process, uint thread, void* info);

	public unsafe delegate DebugContinueStatus DebugEventCallback(uint process, uint thread, void* info);

	protected IntPtr CallbackHandle;

	protected DebugEventHandler()
	{
		CallbackHandle = CreateDebugLoopCallbacks();
		unsafe
		{
			SetDebugLoopCallback(CallbackHandle, 1, OnException);
			SetDebugLoopCallback(CallbackHandle, 2, OnCreateThread);
			SetDebugLoopCallback(CallbackHandle, 3, OnCreateProcess);
			SetDebugLoopCallback(CallbackHandle, 4, OnExitThread);
			SetDebugLoopCallback(CallbackHandle, 5, OnExitProcess);
			SetDebugLoopCallback(CallbackHandle, 6, OnLoadDll);
			SetDebugLoopCallback(CallbackHandle, 7, OnUnloadDll);
			SetDebugLoopCallback(CallbackHandle, 8, OnOutputDebugString);
			SetDebugLoopCallback(CallbackHandle, 9, OnRip);
		}
	}

	~DebugEventHandler()
	{
		DestroyDebugLoopCallbacks(CallbackHandle);
		Release();
	}

	[DllImport(InteropConfig.Api64, EntryPoint = "CreateDebugLoopCallbacks")]
	protected static extern IntPtr CreateDebugLoopCallbacks();

	[DllImport(InteropConfig.Api64, EntryPoint = "SetDebugLoopCallback")]
	protected static extern void SetDebugLoopCallback(IntPtr callbacks, int eventId,
		[MarshalAs(UnmanagedType.FunctionPtr)] DebugEventCallback callback);

	[DllImport(InteropConfig.Api64, EntryPoint = "DestroyDebugLoopCallbacks")]
	protected static extern void DestroyDebugLoopCallbacks(IntPtr callbacks);

	[DllImport(InteropConfig.Api64, EntryPoint = "DebugLoopWaitEvent")]
	protected static extern DebugContinueStatus DebugLoopWaitEvent(IntPtr callbacks);

	public override void OnRelease()
	{
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct ExceptionRecord
{
	public uint ExceptionCode;
	public uint ExceptionFlags;
	public IntPtr NextException; // ExceptionRecord
	public IntPtr ExceptionAddress;
	public uint NumberParameters;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.SysInt)]
	public ulong* ExceptionInformation;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ExceptionDebugInfo
{
	public ExceptionRecord ExceptionRecord;
	public uint dwFirstChance;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct CreateThreadDebugInfo
{
	public IntPtr hThread;
	public IntPtr lpThreadLocalBase;
	public IntPtr lpStartAddress;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct CreateProcessDebugInfo
{
	public IntPtr hFile;
	public IntPtr hProcess;
	public IntPtr hThread;
	public IntPtr lpBaseOfImage;
	public uint dwDebugInfoFileOffset;
	public uint nDebugInfoSize;
	public IntPtr lpThreadLocalBase;
	public IntPtr lpStartAddress;
	public IntPtr lpImageName;
	public ushort fUnicode;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ExitProcessDebugInfo
{
	public uint dwExitCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ExitThreadDebugInfo
{
	public uint dwExitCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct LoadDllDebugInfo
{
	public IntPtr hFile;
	public IntPtr lpBaseOfDll;
	public uint dwDebugInfoFileOffset;
	public uint nDebugInfoSize;
	public IntPtr lpImageName;
	public ushort fUnicode;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct UnloadDllDebugInfo
{
	public IntPtr lpBaseOfDll;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct OutputDebugStringInfo
{
	[MarshalAs(UnmanagedType.LPStr)] public IntPtr lpDebugStringData;
	public ushort fUnicode;
	public ushort nDebugStringLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct RipInfo
{
	public uint dwError;
	public uint dwType;
}