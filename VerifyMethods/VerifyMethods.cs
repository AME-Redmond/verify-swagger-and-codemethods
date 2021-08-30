// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

namespace VerifyMethods
{
    public class VerifyMethods
    {
        HashSet<string> codeMethods = new HashSet<string>();
        HashSet<string> addedMethodNames = new HashSet<string>();
        HashSet<string> swaggerMethods = new HashSet<string>(); //outside the foreach file loop to accumulate from all files
        string assemblyName = "";

        public void VerifySwaggerAndMethods(string swaggerFilepath, string dllPath)
        {
            Console.WriteLine(swaggerFilepath);
            Console.WriteLine(dllPath);

            //TODO: need to read in a list of files from a source
            string jsonContent = File.ReadAllText(swaggerFilepath);
            dynamic doc = JsonConvert.DeserializeObject(jsonContent);           
            foreach (dynamic path in doc.paths)
            {
                foreach(dynamic verb in path.Value)
                {
                    string methodName = verb.Value.operationId.ToString();

                    if (methodName.Contains("Operations_"))
                        continue;

                    if (methodName.Contains("ListAll") || methodName.Contains("List"))
                        methodName = methodName.Replace("List", "Get");

                    swaggerMethods.Add(methodName);
                }
            }

            string myNamespace = this.GetType().Namespace;
            string sdkNamespace = myNamespace.Substring(0, myNamespace.Length - 6); //get rid of ".Tests"

            var assembly = Assembly.LoadFrom(dllPath);//AppDomain.CurrentDomain.Load(dll);
            Console.WriteLine(assembly.FullName);
            assemblyName = assembly.GetName().Name;
            var allTypes = assembly.GetTypes();
            var typesInNamespace = allTypes.Where(t => t.Namespace == sdkNamespace);
            var extensionTypes = allTypes.Where(t => t.Name.Contains("Extensions"));
            var filteredTypeList = typesInNamespace.Where(t => t.IsPublic && InheritsFromResourceOperations(t));

            foreach (var type in extensionTypes)
            {
                foreach (var method in type.GetMethods().Where(m => m.IsPublic && m.IsStatic))
                {
                    AddMethodsFromExtensionClass(type, method);
                }
            }

            foreach (var type in filteredTypeList)
            {
                foreach (var method in type.GetMethods().Where(m => m.IsPublic && m.IsVirtual))
                {
                    AddMethodsFromResourceClass(type, method);
                }
            }

            ExportLinesToTxt();

            AreEqual(swaggerMethods, codeMethods);
        }

        private bool InheritsFromResourceContainer(Type t)
        {
            if (t is null)
                return false;

            if (t.BaseType.Name == "Object")
                return false;

            if (t.BaseType.Name == "ResourceContainer")
                return true;

            return InheritsFromResourceContainer(t.BaseType);
        }

        private bool AreEqual(HashSet<string> methodsFromSwagger, HashSet<string> methodsFromCode)
        {   
            List<string> intersection = new List<string>();

            string[] lines = new string[Math.Max(methodsFromSwagger.Count, methodsFromCode.Count)];
            int index = 0;

            foreach (var methodFromSwagger in methodsFromSwagger)
            {
                if (methodsFromCode.Contains(methodFromSwagger))
                {
                    intersection.Add(methodFromSwagger);
                    lines[index] = methodFromSwagger;
                    index++;
                }
            }
            File.WriteAllLines($"../{assemblyName}matchinglines.txt", lines);

            index = 0;
            string[] mismatchedLines = new string[Math.Max(methodsFromSwagger.Count, Math.Max(addedMethodNames.Count, methodsFromCode.Count))];
            
            List<string> unmatchedCodeIds = new List<string>();
            List<string> unmatchedSwaggerIds = new List<string>();

            foreach (var methodFromSwagger in methodsFromSwagger)
            {
                if (intersection.Contains(methodFromSwagger))
                    continue;

                unmatchedSwaggerIds.Add(methodFromSwagger);
                string old = mismatchedLines[index];
                mismatchedLines[index] = methodFromSwagger + old;
                index++;
            }

            index = 0;
            foreach (var methodFromCode in methodsFromCode)
            {
                if (intersection.Contains(methodFromCode))
                    continue;

                unmatchedCodeIds.Add(methodFromCode);
                string old = mismatchedLines[index];
                if (old == null)
                    mismatchedLines[index] = "," + methodFromCode;
                else
                    mismatchedLines[index] = old + "," + methodFromCode;
                index++;
            }

            index = 0;
            foreach (var addedMethod in addedMethodNames)
            {
                string old = mismatchedLines[index];
                if (old == null)
                    mismatchedLines[index] = ",," + addedMethod;
                else if (old.Contains(","))
                    mismatchedLines[index] = old + "," + addedMethod;
                else
                    mismatchedLines[index] = old + ",," + addedMethod;

                index++;
            }

            FindMatches(unmatchedSwaggerIds, unmatchedCodeIds);
            File.WriteAllLines($"../{assemblyName}mismatchedLines.txt", mismatchedLines);

            return true;
        }

        private bool InheritsFromResourceOperations(Type t)
        {
            if (t is null)
                return false;

            string baseTypeName = t.BaseType.Name;
            if (baseTypeName == "Object")
                return false;

            if (baseTypeName == "ResourceOperations")
                return true;

            return InheritsFromResourceOperations(t.BaseType);
        }

        private bool FindMatches(List<string> methodsFromSwagger, List<string> methodsFromCode)
        {
            List<string> swagger = new List<string>();
            for (int i = 0; i < methodsFromSwagger.Count; i++)
            {
                string x = methodsFromSwagger[i].Replace("_", "");
                x = x.Replace("By", "");
                x = x.Replace("VM", "Vm");
                List<string> firstSplit = Regex.Split(x, @"(?<!^)(?=[A-Z])").ToList();
                firstSplit.Sort();
                swagger.Add(String.Join("_", firstSplit));
            }

            List<string> codes = new List<string>();
            for (int i = 0; i < methodsFromCode.Count; i++)
            {
                string x = methodsFromCode[i].Replace("_", "");
                x = x.Replace("Extensions", "");
                x = x.Replace("By", "");
                x = x.Replace("VM", "Vm");
                List<string> firstSplit = Regex.Split(x, @"(?<!^)(?=[A-Z])").ToList();
                firstSplit.Sort();
                codes.Add(String.Join("_", firstSplit));
            }
            string[] mismatchedLines = new string[Math.Max(swagger.Count, codes.Count)];

            List<string> matches = new List<string>();
            List<string> mismatch = new List<string>();
            for (int i = 0; i < swagger.Count; i++)
            {
                if (codes.Contains(swagger[i]))
                {
                    matches.Add(swagger[i]);
                    continue;
                }

                mismatchedLines[i] = swagger[i];
                mismatch.Add(swagger[i]);
            }

            for (int i = 0; i < codes.Count; i++)
            {
                if (swagger.Contains(codes[i]))
                {
                    if (matches.Contains(codes[i]))
                        continue;

                    matches.Add(codes[i]);
                    continue;
                }

                string old = mismatchedLines[i];
                if (old == null)
                    mismatchedLines[i] = "," + codes[i];
                else 
                    mismatchedLines[i] = old + "," + codes[i];

                mismatch.Add(codes[i]);
            }
            
            File.WriteAllLines($"../{assemblyName}secondPass.txt", mismatchedLines);

            return true;
        }

        private void AddMethodsFromExtensionClass(Type t, MethodInfo method)
        {
            string typeName = t.Name;
            string methodName = method.Name;

            //TODO: Convert to static hashset lookup against the full name
            if (!typeName.Contains("ResourceGroup") &&
                !typeName.Contains("Subscription") &&
                !typeName.Contains("ArmClient") &&
                !typeName.Contains("Management") &&
                !typeName.Contains("Tenant"))
                return;

            if (methodName.EndsWith("Async"))
                return;

            if (InheritsFromResourceContainer(method.ReturnType))
                return;

            if (typeName.Contains("Container"))
                typeName = typeName.Replace("Container", "s");

            if (typeName.Contains("Operation"))
                typeName = typeName.Replace("Operation", "");
            
            codeMethods.Add($"{typeName}_{methodName}");
        }

        private void AddMethodsFromResourceClass(Type t, MethodInfo method)
        {
            string typeName = t.Name;
            string methodName = method.Name;

            if (methodName.EndsWith("Async"))
                return;

            if (methodName.StartsWith("Start"))
                methodName = methodName.Substring(5);

            if (methodName.StartsWith("get_") ||
                methodName.StartsWith("GetHashCode") ||
                methodName.StartsWith("Equals") ||
                methodName.StartsWith("ToString"))
                return;

            if (!typeName.EndsWith("Operations") &&
                !typeName.EndsWith("Container"))
                return;

            if (methodName.EndsWith("Tag") ||
                methodName.EndsWith("Tags") ||
                methodName.StartsWith("CheckIfExists") ||
                methodName.StartsWith("GetIfExists") ||
                methodName.StartsWith("GetAvailableLocations"))
            {
                addedMethodNames.Add($"{typeName}_{methodName}");
                return;
            }

            if (typeName.Contains("Container"))
                typeName = typeName.Replace("Container", "s");

            if (typeName.Contains("Operation"))
                typeName = typeName.Replace("Operation", "");

            codeMethods.Add($"{typeName}_{methodName}");
        }

        private void ExportLinesToTxt()
        {
            string[] comparisonLines = new string[codeMethods.Count + swaggerMethods.Count];
            int index = 0;

            foreach (var codeMethod in codeMethods)
            {
                comparisonLines[index] = "," + codeMethod;
                index++;
            }

            index = 0;
            foreach (var swaggerMethod in swaggerMethods)
            {
                string old = comparisonLines[index];
                comparisonLines[index] = swaggerMethod + old;
                index++;
            }

            //print both sets of lines - Swagger methods, methods from codes to a file named comparisonLines
            File.WriteAllLines($"../{assemblyName}comparisonLines.txt", comparisonLines);
        }
    }
}
