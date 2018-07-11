using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace VSXMLParser {
	/// <summary>An example generic class.</summary>
	/// <typeparam name="T">This is the generic typeparam for AGenericClassExample</typeparam>
	public class AGenericClassExample<T> {

		/// <summary>This is a generic function</summary>
		/// <typeparam name="T1">This little sucker does a thing</typeparam>
		/// <typeparam name="T2">So does htis one</typeparam>
		/// <param name="a">'a' just so happens to be corresponding to T1</param>
		/// <param name="b">And b just so happens to be corresponding to T2</param>
		/// <returns>default(T)</returns>
		public T SomeGenericFunction<T1, T2>(T1 a, T2 b) {
			return default(T);
		}
	}

	/// <summary>Main program</summary>
	class Program {
		/// <summary>The main entry point to the application.</summary>
		/// <param name="args">The string[] of arguments passed to it.</param>
		static void Main(string[] args) {
			Catch(() => { // catch any exceptions using a premade function
#if DEBUG
				args = new string[]{ "VSXMLParser.xml" };
#endif

				if (!(args?.Length > 0 && File.Exists(args[0]))) throw new Exception("Please specify the path to a file as the first argument");

				var fileInput = args[0];
				var fileOutput = $"{Path.GetFileNameWithoutExtension(Path.GetFullPath(args[0]))}.md";

				using (var stream = File.Open(args[0], FileMode.OpenOrCreate))
				using (var outputStream = File.Open(fileOutput, FileMode.OpenOrCreate))
				using (var sw = new StreamWriter(outputStream)) {
					Catch(() => {
						var root = XElement.Load(stream);

						var assemblyName = root.Element("assembly")?.Element("name") ?? throw new Exception("Unable to find an assembly/name element");
						var members = root.Element("members")?.Elements("member") ?? throw new Exception("Unable to find members element");

						var docs = GetItems(members);

						Catch(() => {
							IParser parser = new MarkdownParser();

							parser.Parse(docs, sw);
						}, "Parser Error!");
					}, "XML Parsing Error!");
				}
			}, "Main Application Error!");

#if DEBUG
			Console.ReadLine();
#endif
		}

		/// <summary>Turn an IEnumerable of XElements into an array of DocumentedItems</summary>
		/// <param name="members">The IEnumerable to enumerate over</param>
		/// <returns>A nice clean array of DocumentedItems</returns>
		static DocumentedItem[] GetItems(IEnumerable<XElement> members) {
			var docs = new List<DocumentedItem>();

			foreach (var i in members) {
				var prms = i.Elements("param")?.Select(x => new Param {
					Name = x?.Attribute("name")?.Value,
					Summary = x?.Value
				})?.Concat(i.Elements("typeparam")?.Select(x => new Param {
					Name = x?.Attribute("name")?.Value,
					Summary = x?.Value
				}))?.ToArray();

				docs.Add(new DocumentedItem {
					Name = DocumentedItem.ParseName(i.Attribute("name")?.Value, prms),

					Summary = i.Element("summary")?.Value,
					Remarks = i.Element("remarks")?.Value,
					Returns = i.Element("returns")?.Value,

					Params = prms
				});
			}

			return docs.ToArray();
		}

		/// <summary>Catches a method incase of failure</summary>
		/// <param name="method">The action to wrap in a try catch block</param>
		/// <param name="name">The message to tell the user upon failure</param>
		static void Catch(Action method, string name) {
			try {
				method?.Invoke();
			} catch (Exception e) {
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine(name);
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Error \"{e.Message}\"{new string('\n', 4)}{e.StackTrace}");
				Console.ResetColor();
			}
		}
	}

	/// <summary>A DocumentedItem. Wraps each member item in the XML document into a nice class.</summary>
	public class DocumentedItem {
		/// <summary>Parse backticks into actual readable generics</summary>
		/// <param name="name">The name to parse</param>
		/// <param name="prms">The list of parameters to use as input for the names</param>
		/// <returns>A string with &gt;T&lt;s everywhere</returns>
		public static string ParseName(string name, Param[] prms) {
			if (prms.Length > 0) {
				int prmOn = 0;
				int searchAt = 0;
				foreach (var i in prms) {
					if ((searchAt = name.IndexOf(',', searchAt)) < 0) break;

					name = name.ReplaceFirst(",", " " + (i?.Name ?? "") + ", ", searchAt);

					searchAt += (i?.Name ?? "").Length + 3;

					prmOn++;
				}

				if (prms.Length > prmOn)
					name = name.Replace(")", " " + (prms[prmOn]?.Name ?? "") + ")");
			} else name = name.ReplaceAll(", ", ',');

			if (name == null || name.IndexOf('`') < 0) return name;

			name = InternalDoFancyShmancyReplacing("``", "TParam", name);
			name = InternalDoFancyShmancyReplacing("`", "TClass", name);

			return name;
		}

		private static string InternalDoFancyShmancyReplacing(string repl, string t, string name) {
			int p = 0; // first iterate over every type param for the function
			if ((p = name.IndexOf(repl)) > -1) {
				var strb = new StringBuilder();

				int genericAmount = p + repl.Length;
				char c;
				while (name.Length > genericAmount && (c = name[genericAmount++]) >= '0' && c <= '9') strb.Append(c);

				int oldGen = genericAmount;
				if (int.TryParse(strb.ToString(), out genericAmount)) {
					var strbMain = new StringBuilder();
					strbMain.Append($"<");
					for (int i = 1; i <= genericAmount; i++) strbMain.Append($"{t}{i}" + (i == genericAmount ? "" : ", "));
					strbMain.Append(">");

					name = name.ReplaceFirst($"{repl}{genericAmount}", strbMain.ToString());
					for (int i = 0; i < genericAmount; i++) name = name.Replace($"{repl}{i}", $"<{t}{i + 1}>");
				} else throw new Exception($"Unreasonable amount of generics - so many, in fact, there are more then the integer maximum value. Please literally kill the library creator.");
			}

			return name;
		}

		/// <summary>The name</summary>
		public string Name { get; set; }

		/// <summary>A little summary of what it does</summary>
		public string Summary { get; set; }

		/// <summary>Some remarks about it</summary>
		public string Remarks { get; set; }

		/// <summary>What it returns</summary>
		public string Returns { get; set; }

		/// <summary>The parameters associated with it</summary>
		public Param[] Params { get; set; }
	}

	/// <summary>Each param. Provides an overview of it and what it does.</summary>
	public class Param {
		/// <summary>The name of the param</summary>
		public string Name { get; set; }

		/// <summary>A summary of it</summary>
		public string Summary { get; set; }
	}

	internal static class Helper {
		public static string ReplaceFirst(this string text, string search, string replace, int searchAt = 0) {
			int pos = text.IndexOf(search, searchAt);
			if (pos < 0) {
				return text;
			}
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
		}
	}
}