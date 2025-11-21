using System.Reflection;
using BlackBox.Machine;

namespace BlackBox.System;

public static class Shell
{
	private static string GetSimpleTypeName(Type type)
	{
		if (type == typeof(void)) return "void";
		if (type == typeof(int)) return "int";
		if (type == typeof(string)) return "string";
		if (type == typeof(bool)) return "bool";
		if (type == typeof(byte)) return "byte";
		if (type == typeof(char)) return "char";
		if (type == typeof(float)) return "float";
		if (type == typeof(double)) return "double";

		// Handle generic types
		if (type.IsGenericType)
		{
			var genericArgs = type.GetGenericArguments();
			var genericName = type.Name.Substring(0, type.Name.IndexOf('`'));
			var genericParams = string.Join(", ", genericArgs.Select(GetSimpleTypeName));
			return $"{genericName}<{genericParams}>";
		}

		return type.Name;
	}
	
	public static void Help(string function = null)
	{
		string _namespace;
		if (function == null)
		{
			_namespace = "BlackBox.System";
		}
		else
		{
			_namespace = "BlackBox.System."+function; //todo fix to accept classes
		}

		Window.Write(_namespace+":\n\n");

			// Get all types in BlackBox.System namespace
			var assembly = typeof(BlackBox.System.Serial).Assembly;
			var systemTypes = assembly.GetTypes()
				.Where(t => t.Namespace == _namespace && t.IsPublic)
				.OrderBy(t => t.Name)
				.ToList();

			foreach (var type in systemTypes)
			{
				Window.Write($"- {type.Name}\n");

				// Get public static methods
				var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
					.Where(m => !m.IsSpecialName) // Exclude property getters/setters
					.OrderBy(m => m.Name)
					.ToList();

				// Get public static properties
				var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
					.OrderBy(p => p.Name)
					.ToList();

				// Show properties
				foreach (var prop in properties)
				{
					var propType = GetSimpleTypeName(prop.PropertyType);
					Window.Write($"- - {prop.Name}: {propType}\n");
				}

				// Show methods
				for (int i = 0; i < methods.Count; i++)
				{
					var method = methods[i];
					var isLast = (i == methods.Count - 1 && properties.Count == 0);
					var prefix = isLast ? "- - " : "- - ";

					var parameters = string.Join(", ", method.GetParameters().Select(p =>
						$"{GetSimpleTypeName(p.ParameterType)} {p.Name}"));

					var returnType = GetSimpleTypeName(method.ReturnType);

					Window.Write($"{prefix}{method.Name}({parameters}): {returnType}\n");
				}

				Window.Write("-\n");
			}
	}
	
	public static void Clear()
	{
		Window.GetTerminal()?.Clear();
	}

	public static void Reset()
	{
		Sandbox.Reset();
		Window.Write("Sandbox state reset\n");
	}

	public static void Vars()
	{
		var vars = Sandbox.GetVariables().ToList();
		if (vars.Count == 0)
		{
			Window.Write("No variables defined\n");
		}
		else
		{
			Window.Write("Environment Variables:\n");
			foreach (var v in vars)
			{
				Window.Write($"  {v.Name} ({v.Type.Name}) = {v.Value}\n");
			}
		}
	}
}