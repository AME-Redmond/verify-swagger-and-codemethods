using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VerifyMethods
{
    class Program
    {
        public static string artifactsPath = "";

        //args input structure is artifactsPath, swaggerFilepath, dllFilepath
        static void Main(string[] args)
        {
            artifactsPath = args[0];
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            VerifyMethods verify = new VerifyMethods();
            verify.VerifySwaggerAndMethods(args[1], args[2]);            
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string argName = args.Name.Split(",")[0];
            string dllName = argName + ".dll";
            return Assembly.LoadFrom(Path.Combine(artifactsPath, "bin", argName, "Debug", "netstandard2.0", dllName));
        }
    }
}
