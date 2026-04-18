using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SpotRent.Api.Controllers;

public abstract class BaseController<TController> : ControllerBase
{
    protected readonly ILogger<TController> Logger;

    protected BaseController(ILogger<TController> logger)
    {
        Logger = logger;
    }

    protected int? UserId => int.TryParse(
        HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,  out var userId)
            ? userId
            : null;

    protected void Log(
        LogLevel logLevel,
        EventId eventId,
        [StringSyntax("StructuredLogMessageTemplate")]
        string message)
    {
        var action = LoggerMessage.Define(logLevel, eventId, message);
        action.Invoke(Logger, null);
    }

    protected void Log<T1>(
        LogLevel logLevel,
        EventId eventId,
        [StringSyntax("StructuredLogMessageTemplate")]
        string message,
        T1 param1)
    {
        var action = LoggerMessage.Define<T1>(logLevel, eventId, message);
        action.Invoke(Logger, param1, null);
    }
    
    protected void Log<T1, T2>(
        LogLevel logLevel,
        EventId eventId,
        [StringSyntax("StructuredLogMessageTemplate")]
        string message,
        T1 param1,
        T2 param2)
    {
        var action = LoggerMessage.Define<T1, T2>(logLevel, eventId, message);
        action.Invoke(Logger, param1, param2, null);
    }

    protected void Log<T1, T2, T3>(
        LogLevel logLevel,
        EventId eventId,
        [StringSyntax("StructuredLogMessageTemplate")]
        string message,
        T1 param1,
        T2 param2,
        T3 param3)
    {
        var action = LoggerMessage.Define<T1, T2, T3>(logLevel, eventId, message);
        action.Invoke(Logger, param1, param2, param3, null);
    }

    protected void Log<T1, T2, T3, T4>(
        LogLevel logLevel,
        EventId eventId,
        [StringSyntax("StructuredLogMessageTemplate")]
        string message,
        T1 param1,
        T2 param2,
        T3 param3,
        T4 param4)
    {
        var action = LoggerMessage.Define<T1, T2, T3, T4>(logLevel, eventId, message);
        action.Invoke(Logger, param1, param2, param3, param4, null);
    }

    protected void Log<T1, T2, T3, T4 ,T5>(
        LogLevel logLevel,
        EventId eventId,
        [StringSyntax("StructuredLogMessageTemplate")]
        string message,
        T1 param1,
        T2 param2,
        T3 param3,
        T4 param4,
        T5 param5)
    {
        var action = LoggerMessage.Define<T1, T2, T3, T4, T5>(logLevel, eventId, message);
        action.Invoke(Logger, param1, param2, param3, param4, param5, null);
    }
}
