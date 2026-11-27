global using static HappyHomeDesigner.Framework.Logger;
using StardewModdingAPI;

namespace HappyHomeDesigner.Framework;

internal class Logger(IMonitor monitor)
{
	public static void LogInfo(string message)
		=> inst.LogImpl(message, LogLevel.Info);

	public static void LogDebug(string message)
		=> inst.LogImpl(message, LogLevel.Debug);

	public static void LogWarn(string message)
		=> inst.LogImpl(message, LogLevel.Warn);

	public static void LogError(string message)
		=> inst.LogImpl(message, LogLevel.Error);

	public static void LogTrace(string message)
		=> inst.LogImpl(message, LogLevel.Trace);

	public static void LoggerInstanceInitialize(IMonitor m)
		=> inst = new(m);

	private static Logger inst = null!;

	private void LogImpl(string message, LogLevel level)
		=> monitor.Log(message, level);
}
