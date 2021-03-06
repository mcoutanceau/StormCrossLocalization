﻿using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text.RegularExpressions;

namespace Localization.Core
{
	public static class CodeHelper
	{
		public static void WriteToFile(this CodeCompileUnit code, string file, string comment)
		{
			CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
			CodeGeneratorOptions options = new CodeGeneratorOptions
			{
				BlankLinesBetweenMembers = false,
				BracingStyle = "C",
				IndentString = "\t"
			};

			string contentString;
			using (StringWriter stringWriter = new StringWriter())
			{
				provider.GenerateCodeFromCompileUnit(code, stringWriter, options);

				string content = stringWriter.GetStringBuilder().ToString();

				Regex commentRegex = new Regex("<auto-?generated>.*</auto-?generated>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
				contentString = commentRegex.Replace(content, comment);
			}

			if (File.Exists(file))
			{
				using (StreamReader reader = new StreamReader(file))
				{
					string actualContent = reader.ReadToEnd();
					if (actualContent == contentString)
					{
						return;
					}
				}
				File.Delete(file);
			}

			using (StreamWriter writer = new StreamWriter(File.OpenWrite(file)))
			{
				writer.Write(contentString);
			}
		}
	}
}
