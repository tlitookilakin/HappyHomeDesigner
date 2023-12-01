using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HappyHomeDesigner.Framework
{
	public static class ModUtilities
	{
		public static bool TryFindAssembly(string name, [NotNullWhen(true)] out Assembly assembly)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.GetName().Name == name)
				{
					assembly = asm;
					return true;
				}
			}
			assembly = null;
			return false;
		}

		public static bool TryGetGenericOf(this Type type, int index, [NotNullWhen(true)] out Type generic)
		{
			generic = null;
			if (!type.IsGenericType)
				return false;
			var generics = type.GetGenericArguments();
			if (generics.Length <= index)
				return false;
			generic = generics[index];
			return true;
		}
	}
}
