using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Microsoft.Extensions.Hosting;

namespace CscInternalVisible
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder().RunConsoleAppFrameworkAsync<CscManagerBase>(args);
        }
    }

    public class CscManagerBase : ConsoleAppBase
    {
        private const string RootDirectoryPath = "./";
        private const string DirectoryPath = RootDirectoryPath + "tools/";
        private const string TargetDllFolderPath = DirectoryPath + "tools/";
        private const string TargetDllName = "Microsoft.CodeAnalysis.CSharp.dll";

        private const string CopySuffix = ".copy";
        private const string BytesSuffix = ".bytes";

        private void EnableInternalAccess_One(string directory) => EnableInternalAccess(directory);

        [Command("enable", "Enables csc to process internal access")]
        public
        void
            EnableInternalAccess
        (
            [Option("directory", "Directory contains Microsoft.CodeAnalysis.CSharp.dll")] string directory = TargetDllFolderPath,
            [Option("path", "Microsoft.CodeAnalysis.CSharp.dll path")] string path = "",
            [Option("flag", "TopLevelBinderFlags")] uint binderFlags = 0x400000
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine("Directory : " + directory);
                    path = Path.Combine(directory, TargetDllName);
                }
                else
                {
                    Console.WriteLine("File : " + path);
                }

                PrepareFile(path);

                using (var module = ReadModule(directory, path, false))
                {
                    AddInternalVisibility(module, path, binderFlags);
                }

                ExchangeFile(path);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        [Command("enable-vscode", "Enables csc to process internal access of Visual Studio Code")]
        public void EnableInternalAccessVsCode()
        {
            try
            {
                ProcessForEachVsCodeOmnisharpExtensions(EnableInternalAccess_One);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        private void Disable_One(string directory = TargetDllFolderPath) => Disable(directory);

        [Command("disable", "Disable_One csc not to process internal access any more")]
        public void Disable(
                [Option("directory", "Directory contains Microsoft.CodeAnalysis.CSharp.dll")]
                string directory = TargetDllFolderPath,
                [Option("path", "Microsoft.CodeAnalysis.CSharp.dll path")]
                string path = ""
            )
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("Directory : " + directory);
                path = Path.Combine(directory, TargetDllName);
            }
            else
            {
                Console.WriteLine("File : " + path);
            }
            PrepareFile(path);
        }

        [Command("disable-vscode", "Disable_One csc not to process internal access any more")]
        public void
            DisableVsCode()
        {
            try
            {
                ProcessForEachVsCodeOmnisharpExtensions(Disable_One);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        private static void ProcessForEachVsCodeOmnisharpExtensions(Action<string> ProcessDictionary)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.None);
            if (string.IsNullOrWhiteSpace(home))
                throw new DirectoryNotFoundException(Environment.SpecialFolder.UserProfile.ToString());
            var vsCode = Path.Combine(home, ".vscode", "extensions");
            foreach (var msVsCodeCsharp in Directory.EnumerateDirectories(vsCode, "ms-vscode.csharp-*"))
            {
                string omnisharp = Path.Combine(msVsCodeCsharp, "." + nameof(omnisharp));
                foreach (var versionDirectoryUnderOmnisharp in Directory.EnumerateDirectories(omnisharp, "*"))
                {
                    ProcessDictionary(versionDirectoryUnderOmnisharp);
                }
            }
        }

        private static void ExchangeFile(string path)
        {
            var byteFileName = path + BytesSuffix;
            if (File.Exists(byteFileName))
                File.Delete(byteFileName);
            File.Move(path, byteFileName);
            File.Move(path + CopySuffix, path);
        }

        private static void AddInternalVisibility(ModuleDefinition module, string path, uint binderFlags)
        {
            if ((module.Attributes & ModuleAttributes.ILLibrary) != 0)
            {
                module.Attributes = (module.Attributes ^ ModuleAttributes.ILLibrary) | ModuleAttributes.ILOnly;
                Console.WriteLine(module.Attributes.ToString());
            }

            var csharpCompilationOptions = module.GetType("Microsoft.CodeAnalysis.CSharp", "CSharpCompilationOptions");
            var topLevelBinderFlagsField = csharpCompilationOptions.Fields.Single(x => x.FieldType.Name == "BinderFlags");

            var compilationOptions = csharpCompilationOptions.BaseType.Resolve();
            var metaDataImportOptionsProperty = compilationOptions.Properties.Single(x => x.PropertyType.Name == "MetadataImportOptions");
            var metaDataImportOptionsSetter = module.ImportReference(metaDataImportOptionsProperty.SetMethod);
            var metaDataImportOptionsGetter = module.ImportReference(metaDataImportOptionsProperty.GetMethod);

            var constructors = csharpCompilationOptions.Methods.Where(x => x.IsConstructor);
#if DEBUG
            var console = new TypeReference("System", "Console", module, module.TypeSystem.CoreLibrary, false);
            var writeLine = new MethodReference("WriteLine", module.TypeSystem.Void, console)
            {
                Parameters = { new ParameterDefinition(module.TypeSystem.Object) }
            };
#endif
            foreach (var constructor in constructors)
            {
#if DEBUG
                EnableIgnoreAccessibility(constructor, topLevelBinderFlagsField, metaDataImportOptionsSetter, binderFlags, metaDataImportOptionsGetter, writeLine);
#else
                EnableIgnoreAccessibility(constructor, topLevelBinderFlagsField, metaDataImportOptionsSetter, binderFlags);
#endif
            }
            RewriteSetter(csharpCompilationOptions, binderFlags);
            module.Write(path + CopySuffix);
        }

        private static void PrepareFile(string path)
        {
            var bytePath = path + BytesSuffix;
            if (!File.Exists(bytePath)) return;
            if (File.Exists(path))
                File.Delete(path);
            File.Move(bytePath, path);
        }

        private static void RewriteSetter(TypeDefinition csharpCompilationOptions, uint flag)
        {
            var property = csharpCompilationOptions.Properties.First(x => x.PropertyType.Name == "BinderFlags");
            var setter = property.SetMethod;
            var processor = setter.Body.GetILProcessor();
            var instructions = setter.Body.Instructions;

            var adds = new[]
            {
                Instruction.Create(OpCodes.Ldc_I4, (int)flag),
                Instruction.Create(OpCodes.Or),
            };
            InsertBefore(processor, instructions[instructions.Count - 2], adds);
        }

        private static ModuleDefinition ReadModule(string directory, string path, bool readWrite)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(directory);
            var module = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                ReadWrite = readWrite,
                AssemblyResolver = resolver,
            }).MainModule;
            return module;
        }

        private static void EnableIgnoreAccessibility(MethodDefinition constructor,
            FieldReference topLevelBinderFlagsField,
            MethodReference metaDataImportOptionsSetter,
            uint binderFlags
#if DEBUG
            ,
            MethodReference metaDataImportOptionsGetter,
            MethodReference writeLine
#endif
        )
        {
            var processor = constructor.Body.GetILProcessor();
            var instructions = constructor.Body.Instructions;
            for (var i = instructions.Count - 1; i >= 0; i--)
            {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Ret) continue;

                var adds = new[]
                {
                    #if DEBUG
                    Instruction.Create(OpCodes.Ldstr, "before : "),
                    Instruction.Create(OpCodes.Call, writeLine),
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldfld, topLevelBinderFlagsField),
                    Instruction.Create(OpCodes.Box, topLevelBinderFlagsField.FieldType),
                    Instruction.Create(OpCodes.Call, writeLine),
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, metaDataImportOptionsGetter),
                    Instruction.Create(OpCodes.Box, metaDataImportOptionsGetter.ReturnType),
                    Instruction.Create(OpCodes.Call, writeLine),
                    #endif
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldc_I4, (int) binderFlags),
                    Instruction.Create(OpCodes.Stfld, topLevelBinderFlagsField),
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldc_I4_2),
                    Instruction.Create(OpCodes.Call, metaDataImportOptionsSetter),
                    #if DEBUG
                    Instruction.Create(OpCodes.Ldstr, "after : "),
                    Instruction.Create(OpCodes.Call, writeLine),
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldfld, topLevelBinderFlagsField),
                    Instruction.Create(OpCodes.Box, topLevelBinderFlagsField.FieldType),
                    Instruction.Create(OpCodes.Call, writeLine),
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, metaDataImportOptionsGetter),
                    Instruction.Create(OpCodes.Box, metaDataImportOptionsGetter.ReturnType),
                    Instruction.Create(OpCodes.Call, writeLine),
                    #endif
                };
                InsertBefore(processor, instruction, adds);
            }
        }

        private static void InsertBefore(ILProcessor processor, Instruction instruction, Instruction[] adds)
        {
            processor.InsertBefore(instruction, adds[0]);
            for (var j = 1; j < adds.Length; j++)
            {
                processor.InsertAfter(adds[j - 1], adds[j]);
            }
        }
    }
}
