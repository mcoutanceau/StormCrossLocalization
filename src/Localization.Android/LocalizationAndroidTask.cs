﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Localization.Core;

namespace Localization.Android
{
	public class LocalizationAndroidTask : BaseLocalizationTask
	{
		private const string SERVICE_FILE = Constants.IMPLEMENTATION_SERVICE_NAME + Constants.FILE_SUFFIX;
		private const string LOCALIZED_STRINGS_FILE = Constants.LOCALIZED_STRINGS_NAME + Constants.FILE_SUFFIX;

		protected override void Generate(Dictionary<string, List<ResxFile>> files)
		{
			List<string> keys = new List<string>();

			foreach (string directory in files.Keys)
			{
				GenerateStrings(directory, files[directory]);

				keys.AddRange(files[directory].SelectMany(x => x.Content.Keys.Select(y => y.SimplifyKey())));
			}

			keys = keys.Distinct().ToList();

			GenerateLocalizationService(keys);
			GenerateLocalizedStrings(keys);

			base.Generate(files);
		}

		protected virtual void GenerateStrings(string directory, List<ResxFile> files)
		{
			Dictionary<string, string> content = new Dictionary<string, string>();
			Dictionary<string, string> platformSpecificContent = new Dictionary<string, string>();

			foreach (var item in files.SelectMany(x => x.Content))
			{
				if (item.Key.IsPlatformSpecificString())
				{
					if (item.Key.IsAndroidString())
					{
						platformSpecificContent.Add(item.Key, ProcessValue(item.Value));
					}
				}
				else
				{
					content.Add(item.Key, ProcessValue(item.Value));
				}
			}

			foreach (var item in platformSpecificContent)
			{
				string key = item.Key.SimplifyKey();
				if (content.ContainsKey(key))
				{
					content[key] = item.Value;
				}
				else
				{
					content.Add(key, item.Value);
				}
			}

			//write strings.xml file
			XmlDocument document = new XmlDocument();
			document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
			XmlNode rootNode = document.CreateElement("resources");
			document.AppendChild(rootNode);

			foreach (var pair in content)
			{
				XmlNode elementNode = document.CreateElement("string");
				elementNode.InnerText = pair.Value;
				XmlAttribute attributeName = document.CreateAttribute("name");
				attributeName.Value = pair.Key;
				// ReSharper disable once PossibleNullReferenceException
				elementNode.Attributes.Append(attributeName);

				rootNode.AppendChild(elementNode);
			}

			document.Save(Path.Combine(directory, "strings.xml"));
		}

		protected virtual void GenerateLocalizationService(List<string> keys)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();

			// for class declaration
			CodeNamespace codeNamespace = new CodeNamespace(GenerationNamespace);
			codeUnit.Namespaces.Add(codeNamespace);

			codeNamespace.Imports.Add(new CodeNamespaceImport(DefaultNamespace));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Android.Content"));

			// create class
			CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(Constants.IMPLEMENTATION_SERVICE_NAME)
			{
				IsClass = true,
				TypeAttributes = TypeAttributes.Public,
			};
			classDeclaration.BaseTypes.Add(Constants.INTERFACE_SERVICE_NAME);
			codeNamespace.Types.Add(classDeclaration);

			const string fieldName = "_ctx";

			//field
			CodeMemberField field = new CodeMemberField("Context", fieldName)
			{
				Attributes = MemberAttributes.Private
			};
			classDeclaration.Members.Add(field);

			//constructor
			CodeConstructor constructor = new CodeConstructor();
			constructor.Parameters.Add(new CodeParameterDeclarationExpression("Context", "ctx"));
			constructor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodeVariableReferenceExpression("ctx")));
			classDeclaration.Members.Add(constructor);

			//methode
			var method = new CodeMemberMethod
			{
				Name = Constants.SERVICE_METHOD_NAME,
				ReturnType = new CodeTypeReference(typeof(string)),
				Attributes = MemberAttributes.Public
			};
			method.Parameters.Add(new CodeParameterDeclarationExpression(Constants.ENUM_NAME, "key"));
			classDeclaration.Members.Add(method);

			CodeVariableReferenceExpression methodParam = new CodeVariableReferenceExpression("key");
			CodeFieldReferenceExpression contextReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
			CodeMethodReferenceExpression getStringMethod = new CodeMethodReferenceExpression(contextReference, "GetString");

			CodeTypeReferenceExpression resourceStringReference = new CodeTypeReferenceExpression("Resource.String");

			foreach (string key in keys)
			{
				CodeConditionStatement condition = new CodeConditionStatement(
					new CodeBinaryOperatorExpression(
						methodParam,
						CodeBinaryOperatorType.IdentityEquality,
						new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(Constants.ENUM_NAME), key)
						),
					new CodeMethodReturnStatement(
						new CodeMethodInvokeExpression(getStringMethod, new CodePropertyReferenceExpression(resourceStringReference, key))
						)
					);

				method.Statements.Add(condition);
			}

			method.Statements.Add(new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(ArgumentOutOfRangeException))));

			codeUnit.WriteToFile(SERVICE_FILE, "This file was generated by Localization task for Android");
		}

		protected virtual void GenerateLocalizedStrings(List<string> keys)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();

			// for class declaration
			CodeNamespace codeNamespace = new CodeNamespace(GenerationNamespace);
			codeUnit.Namespaces.Add(codeNamespace);

			codeNamespace.Imports.Add(new CodeNamespaceImport(DefaultNamespace));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Android.Content"));

			// create class
			CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(Constants.LOCALIZED_STRINGS_NAME)
			{
				IsClass = true,
				TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed,
			};
			codeNamespace.Types.Add(classDeclaration);

			const string fieldName = "_ctx";

			//field
			CodeMemberField field = new CodeMemberField("Context", fieldName)
			{
				Attributes = MemberAttributes.Private | MemberAttributes.Static
			};
			classDeclaration.Members.Add(field);

			//initialize method
			CodeMemberMethod initializeMethod = new CodeMemberMethod
			{
				Name = "Initialize",
				Attributes = MemberAttributes.Public | MemberAttributes.Static
			};
			initializeMethod.Parameters.Add(new CodeParameterDeclarationExpression("Context", "ctx"));
			initializeMethod.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(Constants.LOCALIZED_STRINGS_NAME), fieldName), new CodeVariableReferenceExpression("ctx")));
			classDeclaration.Members.Add(initializeMethod);

			//constructor
			CodeConstructor constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Private
			};
			classDeclaration.Members.Add(constructor);

			CodeFieldReferenceExpression contextReference = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(Constants.LOCALIZED_STRINGS_NAME), fieldName);
			CodeMethodReferenceExpression getStringMethod = new CodeMethodReferenceExpression(contextReference, "GetString");

			CodeTypeReferenceExpression resourceStringReference = new CodeTypeReferenceExpression("Resource.String");

			//properties
			foreach (string key in keys)
			{
				CodeMemberProperty property = new CodeMemberProperty
				{
					Name = key,
					Attributes = MemberAttributes.Public | MemberAttributes.Static
				};

				property.GetStatements.Add(new CodeMethodReturnStatement(
					new CodeMethodInvokeExpression(getStringMethod, new CodePropertyReferenceExpression(resourceStringReference, key))
					));
				classDeclaration.Members.Add(property);
			}

			codeUnit.WriteToFile(LOCALIZED_STRINGS_FILE, "This file was generated by Localization task for Android");
		}

		private string ProcessValue(string value)
		{
			return value.Replace("'", "\\'");
		}
	}
}
