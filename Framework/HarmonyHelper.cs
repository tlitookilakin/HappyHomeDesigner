using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Linq;
using System.Reflection;

namespace HappyHomeDesigner.Framework;

/// <summary>
/// A simplified, chainable tool to easily apply harmony patches.
/// </summary>
/// <param name="Harmony">The harmony instance to use</param>
/// <param name="Monitor">The mod's monitor</param>
public class HarmonyHelper(Harmony Harmony, IMonitor Monitor)
{
	public const BindingFlags AnyDeclared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
	private static readonly string[] patchTypeNames = ["prefix", "postfix", "transpiler", "finalizer"];

	private readonly MethodInfo[] targets = [];
	private readonly Type targetType;

	private HarmonyHelper(Harmony h, IMonitor m, MethodInfo[] methods, Type type) : this(h, m)
	{
		targets = methods;
		targetType = type;
	}

	public Harmony Patcher => Harmony;

	/// <summary>Set the method and type to patch.</summary>
	/// <param name="name">Method name</param>
	public HarmonyHelper With<T>(string name)
		=> WithImpl(name, false, typeof(T));

	/// <summary>Set the method to patch and use the current type.</summary>
	/// <param name="name">Method name</param>
	public HarmonyHelper With(string name)
		=> WithImpl(name, false);

	/// <summary>Set the property and type to patch.</summary>
	/// <param name="name">property name</param>
	/// <param name="getter">true to patch the getter, false to patch the setter</param>
	public HarmonyHelper WithProperty<T>(string name, bool getter)
		=> WithPropertyImpl(name, getter, false, typeof(T));

	/// <summary>Set the property to patch and use the current type.</summary>
	/// <param name="name">property name</param>
	/// <param name="getter">true to patch the getter, false to patch the setter</param>
	public HarmonyHelper WithProperty(string name, bool getter)
		=> WithPropertyImpl(name, getter, false);

	/// <summary>Set the methods and type to patch.</summary>
	/// <param name="name">The method name</param>
	/// <param name="predicate">A predicate used to determine which overloads should be patched</param>
	public HarmonyHelper WithAll<T>(string name, Func<MethodInfo, bool>? predicate = null)
		=> WithImpl(name, true, typeof(T), predicate);

	/// <summary>Set the methods to patch and use the current type.</summary>
	/// <param name="name">The method name</param>
	/// <param name="predicate">A predicate used to determine which overloads should be patched</param>
	public HarmonyHelper WithAll(string name, Func<MethodInfo, bool>? predicate = null)
		=> WithImpl(name, true, null, predicate);

	/// <summary>Prefix the currently targeted type and method(s)</summary>
	/// <param name="use">The method to patch with</param>
	public HarmonyHelper Prefix(Delegate use)
		=> PatchImpl(new(use.Method), 0);

	/// <summary>Postfix the currently targeted type and method(s)</summary>
	/// <param name="use">The method to patch with</param>
	public HarmonyHelper Postfix(Delegate use)
		=> PatchImpl(new(use.Method), 1);

	/// <summary>Transpile the currently targeted type and method(s)</summary>
	/// <param name="use">The method to patch with</param>
	public HarmonyHelper Transpiler(Delegate use)
		=> PatchImpl(new(use.Method), 2);

	/// <summary>Apply a finalizer to the currently targeted type and method(s)</summary>
	/// <param name="use">The method to patch with</param>
	public HarmonyHelper Finalizer(Delegate use)
		=> PatchImpl(new(use.Method), 3);

	private HarmonyHelper WithImpl(string name, bool all, Type? target = null, Func<MethodInfo, bool>? predicate = null)
	{
		target ??= targetType;
		if (target is null)
			throw new InvalidOperationException("Must specify target type.");

		MethodInfo[] targs;

		if (all)
		{
			targs = target
				.GetMethods(AnyDeclared | BindingFlags.Static | BindingFlags.Instance)
				.Where(m => m.Name == name && (predicate is null || predicate(m))).ToArray();
		}
		else
		{
			var m = target.GetMethod(name, AnyDeclared | BindingFlags.Static | BindingFlags.Instance);
			targs = m is null ? [] : [m];
		}

		if (targs.Length is 0)
			Monitor.Log($"No method with name '{name}' is declared by type '{target}'.", LogLevel.Error);

		return new(Harmony, Monitor, targs, target);
	}

	private HarmonyHelper WithPropertyImpl(string name, bool getter, bool all, Type? target = null)
	{
		target ??= targetType;
		if (target is null)
			throw new InvalidOperationException("Must specify target type.");

		PropertyInfo[] targs;

		if (all)
		{
			targs = target
				.GetProperties(AnyDeclared | BindingFlags.Static | BindingFlags.Instance)
				.Where(m => m.Name == name && ((!getter && m.CanWrite) || (getter && m.CanRead))).ToArray();
		}
		else
		{
			var m = target.GetProperty(name, AnyDeclared | BindingFlags.Static | BindingFlags.Instance);
			targs = m is null ? [] : [m];
		}

		MethodInfo[] impls = new MethodInfo[targs.Length];
		for (int i = 0; i < targs.Length; i++)
			impls[i] = getter ? targs[i].GetMethod! : targs[i].SetMethod!;

		if (impls.Length is 0)
			Monitor.Log($"No property with name '{name}' is declared by type '{target}'.", LogLevel.Error);

		return new(Harmony, Monitor, impls, target);
	}

	private HarmonyHelper PatchImpl(HarmonyMethod use, int type)
	{
		HarmonyMethod?[] applies = new HarmonyMethod?[4];
		applies[type] = use;

		if (targetType is null)
			throw new InvalidOperationException("Target type must be specified before patching.");

		for (int i = 0; i < targets.Length; i++)
		{
			try
			{
				Harmony.Patch(targets[i], applies[0], applies[1], applies[2], applies[3]);
			}
			catch (Exception e)
			{
				Monitor.Log($"Failed to apply {patchTypeNames[type]} '{use.method}' to '{targets[i]}':\n{e}", LogLevel.Error);
			}
		}

		return this;
	}
}
