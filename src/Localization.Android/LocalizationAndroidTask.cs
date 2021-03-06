﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Localization.Core;

namespace Localization.Android
{
	public class LocalizationAndroidTask : BaseLocalizationTask
	{
		protected override void GenerateForDirectory(string directory, Dictionary<string, string> keyValues)
		{
			//write strings.xml file
			XmlDocument document = new XmlDocument();
			document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
			XmlNode rootNode = document.CreateElement("resources");
			rootNode.AppendChild(document.CreateComment("This file was generated by Localization task for Android"));
			document.AppendChild(rootNode);

			foreach (var pair in keyValues)
			{
				XmlNode elementNode = document.CreateElement("string");
				elementNode.InnerText = pair.Value;
				
				XmlAttribute attributeName = document.CreateAttribute("name");
				attributeName.Value = pair.Key;
				elementNode.Attributes.Append(attributeName);

				XmlAttribute formattedAttribute = document.CreateAttribute("formatted");
				formattedAttribute.Value = "false";
				elementNode.Attributes.Append(formattedAttribute);

				rootNode.AppendChild(elementNode);
			}

			string filepath = Path.Combine(directory, "strings.xml");
			document.SaveIfDifferent(filepath);
			OutputResourceFilePath.Add(filepath);
			
			base.GenerateForDirectory(directory, keyValues);
		}

		protected override void GenerateForProject(List<string> keys)
		{
			GenerateLocalizationService(keys);
			GenerateLocalizedStrings(keys);
			
			base.GenerateForProject(keys);
		}

		protected override string ProcessValue(string value)
		{
			return value?.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("'", "\\'")
				.Replace("\n", "\\n")
				.Replace("\\\\n", "\\n");
		}

		protected override bool IsCurrentPlatformKey(string key) => key.IsAndroidString();

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

			//field
			CodeMemberField field = new CodeMemberField("Context", Constants.CONTEXT_FIELD_NAME)
			{
				Attributes = MemberAttributes.Private
			};
			classDeclaration.Members.Add(field);

			//constructor
			CodeConstructor constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Public
			};
			constructor.Parameters.Add(new CodeParameterDeclarationExpression("Context", Constants.CONTEXT_PARAMETER_NAME));
			constructor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), Constants.CONTEXT_FIELD_NAME), new CodeVariableReferenceExpression(Constants.CONTEXT_PARAMETER_NAME)));
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
			CodeFieldReferenceExpression contextReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), Constants.CONTEXT_FIELD_NAME);
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

			codeUnit.WriteToFile(Constants.IMPLEMENTATION_SERVICE_FILE_PATH, "This file was generated by Localization task for Android");
			OutputCompileFilePath.Add(Constants.IMPLEMENTATION_SERVICE_FILE_PATH);
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

			//field
			CodeMemberField field = new CodeMemberField("Context", Constants.CONTEXT_FIELD_NAME)
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
			initializeMethod.Parameters.Add(new CodeParameterDeclarationExpression("Context", Constants.CONTEXT_PARAMETER_NAME));
			initializeMethod.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(Constants.LOCALIZED_STRINGS_NAME), Constants.CONTEXT_FIELD_NAME), new CodeVariableReferenceExpression(Constants.CONTEXT_PARAMETER_NAME)));
			classDeclaration.Members.Add(initializeMethod);

			//constructor
			CodeConstructor constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Private
			};
			classDeclaration.Members.Add(constructor);

			CodeFieldReferenceExpression contextReference = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(Constants.LOCALIZED_STRINGS_NAME), Constants.CONTEXT_FIELD_NAME);
			CodeMethodReferenceExpression getStringMethod = new CodeMethodReferenceExpression(contextReference, "GetString");

			CodeTypeReferenceExpression resourceStringReference = new CodeTypeReferenceExpression("Resource.String");

			//properties
			foreach (string key in keys)
			{
				CodeMemberProperty property = new CodeMemberProperty
				{
					Name = key,
					Type = new CodeTypeReference(typeof(string)),
					Attributes = MemberAttributes.Public | MemberAttributes.Static
				};

				property.GetStatements.Add(new CodeMethodReturnStatement(
					new CodeMethodInvokeExpression(getStringMethod, new CodePropertyReferenceExpression(resourceStringReference, key))
					));
				classDeclaration.Members.Add(property);
			}

			codeUnit.WriteToFile(Constants.LOCALIZED_STRINGS_FILE_PATH, "This file was generated by Localization task for Android");
			OutputCompileFilePath.Add(Constants.LOCALIZED_STRINGS_FILE_PATH);
		}
	}
}
