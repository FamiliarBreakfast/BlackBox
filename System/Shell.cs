using System.Reflection;
using Sandbox = BlackBox.Machine.Sandbox;
using Window = BlackBox.Window;
using Path = System.IO.Path;

namespace System;

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
	
	// filter: "custom" = just user types, "system" = namespace list, "all" = all classes
	public static void Help(string className = "", string show = "custom")
	{
		if (className == "")
		{
			// Get all assemblies available in the sandbox
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
				.ToList();

			// Get all public types
			var allTypes = assemblies
				.SelectMany(a => {
					try { return a.GetTypes(); }
					catch { return Array.Empty<Type>(); }
				})
				.Where(t => t.Namespace != null && t.IsPublic);

			if (show == "namespace")
			{
				// Show only namespaces for system types
				var namespaces = allTypes
					.Where(t => t.Namespace.StartsWith("System"))
					.Select(t => t.Namespace)
					.Distinct()
					.OrderBy(ns => ns);

				Window.Terminal.Write("Available System namespaces:\n");
				foreach (var ns in namespaces)
				{
					Window.Terminal.Write($"- {ns}\n");
				}
			}
			else
			{
				// Show individual types
				IEnumerable<Type> systemTypes = show switch
				{
					"simple" => allTypes.Where(t =>
						t.Assembly == typeof(Shell).Assembly && t.Namespace.StartsWith("System")),
					"all" => allTypes.Where(t => t.Namespace.StartsWith("System")),
					_ => allTypes.Where(t =>
						t.Assembly == typeof(Shell).Assembly && t.Namespace.StartsWith("System"))
				};

				systemTypes = systemTypes.OrderBy(t => t.Namespace).ThenBy(t => t.Name).ToList();

				string currentNamespace = "";
				foreach (var t in systemTypes)
				{
					if (t.Namespace != currentNamespace)
					{
						currentNamespace = t.Namespace!;
						Window.Terminal.Write($"{currentNamespace}:\n");
					}
					Window.Terminal.Write($"- {t.Name}\n");
				}
			}
		}
		else
		{
			// Search in all assemblies for the specific type
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
				.ToList();

			var type = assemblies
				.SelectMany(a => {
					try { return a.GetTypes(); }
					catch { return Array.Empty<Type>(); }
				})
				.FirstOrDefault(t => t.Namespace != null && t.Namespace.StartsWith("System") && t.Name == className && t.IsPublic);

			if (type == null)
			{
				Window.Terminal.Write($"Class '{className}' not found\n");
				return;
			}

			Window.Terminal.Write($"{type.Name}:\n");

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
				.Where(m => !m.IsSpecialName)
				.OrderBy(m => m.Name)
				.ToList();

			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.OrderBy(p => p.Name)
				.ToList();

			foreach (var prop in properties)
			{
				var propType = GetSimpleTypeName(prop.PropertyType);
				Window.Terminal.Write($"- {prop.Name}: {propType}\n");
			}

			foreach (var method in methods)
			{
				var parameters = string.Join(", ", method.GetParameters().Select(p =>
					$"{GetSimpleTypeName(p.ParameterType)} {p.Name}"));
				var returnType = GetSimpleTypeName(method.ReturnType);
				Window.Terminal.Write($"- {method.Name}({parameters}): {returnType}\n");
			}
		}
	}
	
	public static void Clear()
	{
		Window.Terminal.Clear();
	}

	public static void Reset()
	{
		Sandbox.Reset();
		Window.Terminal.Write("Sandbox state reset\n");
	}

	public static void Vars()
	{
		var vars = Sandbox.GetVariables().ToList();
		if (vars.Count == 0)
		{
			Window.Terminal.Write("No variables defined\n");
		}
		else
		{
			Window.Terminal.Write("Environment Variables:\n");
			foreach (var v in vars)
			{
				Window.Terminal.Write($"  {v.Name} ({v.Type.Name}) = {v.Value}\n");
			}
		}
	}
	
	public static void Evaluate(string code)
	{
		var result = Sandbox.Execute(code);

		if (result.Success)
		{
			if (result.ReturnValue != null)
			{
				Window.Terminal.Write($"=> {result.ReturnValue}\n");
			}
		}
		else
		{
			Window.Terminal.Write($"Error: {result.ErrorMessage}\n");
		}
	}
	
	//File operations
	public static void Read(string path)
	{
		Window.Terminal.Write(new Path(path).Read());
	}
	public static void Write(string path, string text)
	{
		new Path(path).Write(text);
	}

	public static void Execute(string path)
	{
		var result = Sandbox.Execute(new Path(path).Read());

		if (result.Success)
		{
			if (result.ReturnValue != null)
			{
				Window.Terminal.Write($"=> {result.ReturnValue}\n");
			}
		}
		else
		{
			Window.Terminal.Write($"Error: {result.ErrorMessage}\n");
		}
	}
	
	public static void List(string path)
	{
		//list files
	}

	public static void Touch(string path)
	{
		//initialize file
	}
	
	//Move()
	//Copy()
	
}