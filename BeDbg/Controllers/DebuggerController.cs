﻿using BeDbg.Dto;
using BeDbg.Services;
using BeDbg.Util;
using Microsoft.AspNetCore.Mvc;

namespace BeDbg.Controllers;

[Route("[controller]")]
[ApiController]
public class DebuggerController : ControllerBase
{
	private readonly DebugService _debugService;

	public DebuggerController(DebugService debugService)
	{
		_debugService = debugService;
	}

	[HttpGet("{index:int}/event")]
	public async Task ServerSentDebuggingEvent(int index)
	{
		var sender = new ServerEventSender();
		await sender.InitEventAsync(Response);

		var debugger = _debugService.FindOneByIndex(index);
		if (debugger == null)
		{
			await sender.SendEventAsync(new DebuggerEvent
			{
				Event = "notFound",
				Payload = $"Cannot find debugger at index {index}"
			});
			return;
		}


		while (true)
		{
			if (debugger.DebuggerEventList.Count == 0)
			{
				continue;
			}

			var dbgEvent = debugger.DebuggerEventList.Dequeue();
			await sender.SendEventAsync(dbgEvent);
		}
	}
}