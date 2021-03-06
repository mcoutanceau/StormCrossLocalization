﻿using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Localization.Core;

namespace Localization.PCL
{
	public class LocalizationPCLTask : BaseLocalizationTask
	{
		protected override void GenerateForProject(List<string> keys)
		{
			GenerateEnumFields(keys);
			GenerateLocalizationService(keys);
			GenerateLocalizedStrings(keys);
			
			base.GenerateForProject(keys);
		}
		
		protected virtual void GenerateEnumFields(List<string> keys)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			
			// for class declaration
			CodeNamespace codeNamespace = new CodeNamespace(GenerationNamespace);

			codeUnit.Namespaces.Add(codeNamespace);
			
			// create class
			CodeTypeDeclaration enumDeclaration = new CodeTypeDeclaration(Constants.ENUM_NAME)
			{
				IsEnum = true,
				TypeAttributes = TypeAttributes.Public,
			};
			
			codeNamespace.Types.Add(enumDeclaration);

			enumDeclaration.Members.AddRange(keys.Select(key => (CodeTypeMember)new CodeMemberField(Constants.ENUM_NAME, key)).ToArray());

			codeUnit.WriteToFile(Constants.ENUM_FILE_PATH, "This file was generated by Localization task for PCL");
			OutputCompileFilePath.Add(Constants.ENUM_FILE_PATH);
		}

		protected virtual void GenerateLocalizationService(List<string> keys)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();

			// for class declaration
			CodeNamespace codeNamespace = new CodeNamespace(GenerationNamespace);

			codeUnit.Namespaces.Add(codeNamespace);

			// create class
			CodeTypeDeclaration interfaceDeclaration = new CodeTypeDeclaration(Constants.INTERFACE_SERVICE_NAME)
			{
				IsInterface = true,
				TypeAttributes = TypeAttributes.Interface | TypeAttributes.Public,
			};

			codeNamespace.Types.Add(interfaceDeclaration);

			var method = new CodeMemberMethod
			{
				Name = Constants.SERVICE_METHOD_NAME,
				ReturnType = new CodeTypeReference(typeof (string))
			};
			method.Parameters.Add(new CodeParameterDeclarationExpression(Constants.ENUM_NAME, "key"));
			interfaceDeclaration.Members.Add(method);
			
			codeUnit.WriteToFile(Constants.INTERFACE_SERVICE_FILE_PATH, "This file was generated by Localization task for PCL");
			OutputCompileFilePath.Add(Constants.INTERFACE_SERVICE_FILE_PATH);
		}

		protected virtual void GenerateLocalizedStrings(List<string> keys)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			// for class declaration
			CodeNamespace codeNamespace = new CodeNamespace(GenerationNamespace);
			codeUnit.Namespaces.Add(codeNamespace);

			codeNamespace.Imports.Add(new CodeNamespaceImport("System"));


			// create class
			CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(Constants.LOCALIZED_STRINGS_NAME)
			{
				IsClass = true,
				TypeAttributes = TypeAttributes.NestedAssembly | TypeAttributes.Sealed,
			};
			codeNamespace.Types.Add(classDeclaration);

			//private constructor
			CodeConstructor constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Private
			};
			classDeclaration.Members.Add(constructor);

			//static initialize method
			const string fieldName = "_service";

			//field
			CodeMemberField field = new CodeMemberField("Func<ILocalizationService>", fieldName)
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
			initializeMethod.Parameters.Add(new CodeParameterDeclarationExpression("Func<ILocalizationService>", "service"));
			initializeMethod.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(Constants.LOCALIZED_STRINGS_NAME), fieldName), new CodeVariableReferenceExpression("service")));
			classDeclaration.Members.Add(initializeMethod);


			CodeMethodInvokeExpression contextReference = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(Constants.LOCALIZED_STRINGS_NAME), fieldName), "Invoke"));
			CodeMethodReferenceExpression getStringMethod = new CodeMethodReferenceExpression(contextReference, "Get");
			foreach (string key in keys)
			{
				CodeMemberProperty property = new CodeMemberProperty
				{
					Name = key,
					Type = new CodeTypeReference(typeof(string)),
					Attributes = MemberAttributes.Public | MemberAttributes.Static
				};

				property.GetStatements.Add(new CodeMethodReturnStatement(
					new CodeMethodInvokeExpression(getStringMethod, new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(Constants.ENUM_NAME), key))
					));
				classDeclaration.Members.Add(property);
			}


			codeUnit.WriteToFile(Constants.LOCALIZED_STRINGS_FILE_PATH, "This file was generated by Localization task for PCL");
			OutputCompileFilePath.Add(Constants.LOCALIZED_STRINGS_FILE_PATH);
		}

		protected override bool CanHaveDuplicatedKeys => true;
	}
}
