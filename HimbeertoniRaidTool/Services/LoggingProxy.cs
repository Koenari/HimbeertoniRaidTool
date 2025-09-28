using Dalamud.Plugin.Services;
using Serilog;
using Serilog.Events;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class LoggingProxy(ILogger implementation, string prefix) : ILogger
{

    public LoggingProxy(IPluginLog implementation, string prefix) : this(implementation.Logger, prefix) { }

    private string PrefixMessage(string message) => message.Insert(0, Prefix);

    public string Prefix => prefix;

    public void Fatal(string messageTemplate, params object?[]? values) =>
        implementation.Fatal(PrefixMessage(messageTemplate), values);
    public void Fatal(Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Fatal(exception, PrefixMessage(messageTemplate), values);
    public void Error(string messageTemplate, params object?[]? values) =>
        implementation.Error(PrefixMessage(messageTemplate), values);
    public void Error(Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Error(exception, PrefixMessage(messageTemplate), values);

    public void Warning(string messageTemplate, params object?[]? values) =>
        implementation.Warning(PrefixMessage(messageTemplate), values);
    public void Warning(Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Warning(exception, PrefixMessage(messageTemplate), values);
    public void Information(string messageTemplate, params object?[]? values) =>
        implementation.Information(PrefixMessage(messageTemplate), values);
    public void Information(Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Information(exception, PrefixMessage(messageTemplate), values);
    public void Debug(string messageTemplate, params object?[]? values) =>
        implementation.Debug(PrefixMessage(messageTemplate), values);
    public void Debug(Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Debug(exception, PrefixMessage(messageTemplate), values);
    public void Verbose(string messageTemplate, params object?[]? values) =>
        implementation.Verbose(PrefixMessage(messageTemplate), values);
    public void Verbose(Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Verbose(exception, PrefixMessage(messageTemplate), values);
    public void Write(LogEvent logEvent) =>
        Write(logEvent.Level, logEvent.Exception, logEvent.MessageTemplate.Text, logEvent.Properties);
    public void Write(LogEventLevel level, Exception? exception, string messageTemplate, params object?[]? values) =>
        implementation.Write(level, exception, PrefixMessage(messageTemplate), values);
}