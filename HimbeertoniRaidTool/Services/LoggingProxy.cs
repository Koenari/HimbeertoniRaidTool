using Dalamud.Plugin.Services;
using Serilog.Events;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class LoggingProxy : IPluginLog
{
    private readonly IPluginLog _impl;
    internal LoggingProxy(IPluginLog implementation)
    {
        _impl = implementation;
    }

    public LogEventLevel MinimumLogLevel { get => _impl.MinimumLogLevel; set => _impl.MinimumLogLevel = value; }

    public void Fatal(string messageTemplate, params object[] values)
    {
        PrintToChat(LogEventLevel.Fatal, messageTemplate, values);
        _impl.Fatal(messageTemplate, values);
    }
    public void Fatal(Exception? exception, string messageTemplate, params object[] values)
    {
        PrintToChat(LogEventLevel.Fatal, messageTemplate, values);
        _impl.Fatal(exception, messageTemplate, values);
    }
    public void Error(string messageTemplate, params object[] values)
    {
        PrintToChat(LogEventLevel.Error, messageTemplate, values);
        _impl.Error(messageTemplate, values);
    }
    public void Error(Exception? exception, string messageTemplate, params object[] values)
    {
        PrintToChat(LogEventLevel.Error, messageTemplate, values);
        _impl.Error(exception, messageTemplate, values);
    }

    public void Warning(string messageTemplate, params object[] values) => _impl.Warning(messageTemplate, values);
    public void Warning(Exception? exception, string messageTemplate, params object[] values) =>
        _impl.Warning(exception, messageTemplate, values);
    public void Information(string messageTemplate, params object[] values) =>
        _impl.Information(messageTemplate, values);
    public void Information(Exception? exception, string messageTemplate, params object[] values) =>
        _impl.Information(exception, messageTemplate, values);
    public void Info(string messageTemplate, params object[] values) => _impl.Info(messageTemplate, values);
    public void Info(Exception? exception, string messageTemplate, params object[] values) =>
        _impl.Info(exception, messageTemplate, values);
    public void Debug(string messageTemplate, params object[] values) => _impl.Debug(messageTemplate, values);
    public void Debug(Exception? exception, string messageTemplate, params object[] values) =>
        _impl.Debug(exception, messageTemplate, values);
    public void Verbose(string messageTemplate, params object[] values) => _impl.Verbose(messageTemplate, values);
    public void Verbose(Exception? exception, string messageTemplate, params object[] values) =>
        _impl.Verbose(exception, messageTemplate, values);
    public void Write(LogEventLevel level, Exception? exception, string messageTemplate, params object[] values) =>
        _impl.Write(level, exception, messageTemplate, values);

    private void PrintToChat(LogEventLevel level, string messageTemplate, params object[] values)
    {

    }
}