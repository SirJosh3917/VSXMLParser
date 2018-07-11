using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace VSXMLParser {
	/// <summary>An interface for parsers. Allows for easy creation of new parsers</summary>
	public interface IParser {

		/// <summary>The list of documented items to parse</summary>
		/// <param name="items">Each item you gotta parse</param>
		/// <param name="streamWriter">The StreamWriter you can write everything to</param>
		void Parse(DocumentedItem[] items, StreamWriter streamWriter);
	}

	/// <summary>A parser made for Github Flavored Markdown</summary>
	public class MarkdownParser : IParser {

		/// <summary>Parse it all!</summary>
		public void Parse(DocumentedItem[] items, StreamWriter sw) {
			var classes = GetItemsFor(items, "T:", "Classes", sw);
			var methods = GetItemsFor(items, "M:", "Methods", sw);
			var properties = GetItemsFor(items, "P:", "Properties", sw);
			var fields = GetItemsFor(items, "F:", "Fields", sw);

			WriteGroup("Classes", classes, sw);
			WriteGroup("Methods", methods, sw);
			WriteGroup("Properties", properties, sw);
			WriteGroup("Fields", fields, sw);
		}

		public List<DocumentedItem> GetItemsFor(DocumentedItem[] items, string beginning, string tocName, StreamWriter sw) {
			var itms = items.FindThat(beginning);
			itms = itms.OrderBy(x => x.Name).ToList();
			foreach (var i in itms) i.Name = i.Name.Substring(2); // remove the beginning X:
			WriteTOCGroup(tocName, itms, sw);

			return itms;
		}

		private static void WriteTOCGroup(string groupName, IEnumerable<DocumentedItem> items, StreamWriter sw) {
			sw.WriteLine($"- [{groupName}](#{groupName.ToLower()})");
			foreach (var i in items) {
				var visibleName = i.Name?.Replace("`", "\\`") ?? "";
				var hrefName = i.Name?.Replace(' ', '-').Replace(".", "") ?? "";

				if (visibleName != null && hrefName != null)
					sw.WriteLine($"\t- [{visibleName?.Replace("<", "&lt;").Replace(">", "&gt;")}](#{hrefName?.ReplaceAll("", '<', '>', '.', ',', '[', ']', '{', '}', '(', ')').ToLower()})");
			}
			sw.WriteLine("");
			sw.WriteLine("---");
			sw.WriteLine("");
		}

		private static void WriteGroup(string groupName, IEnumerable<DocumentedItem> items, StreamWriter sw) {
			sw.WriteLine($"## {groupName}");
			sw.WriteLine("");
			foreach (var i in items)
				if (i?.Name != null) {
					sw.WriteLine($"{i.Name.Replace("<", "&lt;").Replace(">", "&gt;")}");
					sw.WriteLine($"---");

					WriteDesc(nameof(i.Summary), i.Summary, sw);
					WriteDesc(nameof(i.Remarks), i.Remarks, sw);

					if (i.Params?.Length > 0) {
						const string paramName = "Param Name";
						const string summary = "Summary";

						sw.WriteLine($"{paramName} | {summary}");
						sw.WriteLine($"{new string('-', paramName.Length)} | {new string('-', summary.Length)}");

						foreach (var j in i.Params) {
							if (j?.Name == null) break;
							if (j?.Summary == null) break;

							sw.WriteLine($"{j?.Name.Replace("|", "\\|")} | {j?.Summary.Replace("|", "\\|")}");
						}

						sw.WriteLine("");
					}
				}
		}

		private static void WriteDesc(string name, string value, StreamWriter sw) {
			if (name == null || value == null) return;

			sw.WriteLine($"#### {name}");
			sw.WriteLine($"{value}");
			sw.WriteLine();
		}
	}

	internal static class MarkdownParserHelper {
		public static List<DocumentedItem> FindThat(this DocumentedItem[] itms, string startsWith)
			=> (from x in itms
				where x.Name?.StartsWith(startsWith) is true
				select x).ToList();

		public static string ReplaceAll(this string s, string replaceTo, params char[] validForReplacing) {
			string res = s;
			foreach (var i in validForReplacing)
				res = res.Replace(i.ToString(), replaceTo);
			return res;
		}
	}
}