using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MicroBatchFramework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Microsoft.Extensions.Hosting;

namespace CscInternalVisible
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder().RunBatchEngineAsync<CscManagerBase>(args);
        }
    }

    public class CscManagerBase : BatchBase
    {
        private const string RootDirectoryPath = "./";
        private const string DownloadPath = RootDirectoryPath + "csc.zip";
        private const string DirectoryPath = RootDirectoryPath + "tools/";
        private const string TargetDllFolderPath = DirectoryPath + "tools/";
        private const string TargetDllName = "Microsoft.CodeAnalysis.CSharp.dll";


        [Command("download", "Download csc files from nuget")]
        public
        async Task
            Download
        (
            [Option("version", "C# Compiler Version")] string version = "3.4.0",
            [Option("download-file", "Downloaded Zip File Name")] string download = DownloadPath,
            [Option("directory", "Extended directory directory")] string directory = DirectoryPath
        )
        {
            try
            {
                var url = new Uri(@"https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers/" + version);
                await File.WriteAllBytesAsync(download, await new HttpClient().GetByteArrayAsync(url));

                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
                Directory.CreateDirectory(directory);

                ZipFile.ExtractToDirectory(DownloadPath, directory, true);
                File.Delete(download);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        [Command("enable", "Enables csc to process internal access")]
        public
        void
            EnableInternalAccess
        (
            [Option("directory", "Directory contains Microsoft.CodeAnalysis.CSharp.dll")] string directory = TargetDllFolderPath,
            [Option("path", "Microsoft.CodeAnalysis.CSharp.dll path")] string path = "",
            [Option("flag", "TopLevelBinderFlags")] uint binderFlags = 0x400000,
            [Option("suffix")] string suffix = ""
        )
        {
            try
            {
                Console.WriteLine(directory);
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(directory, TargetDllName);
                Console.WriteLine(path);
                Console.WriteLine(suffix);
                var module = ReadModule(directory, path, string.IsNullOrWhiteSpace(suffix));

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
                    Parameters = {new ParameterDefinition(module.TypeSystem.Object)}
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
                module.Write(path + suffix);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
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

        [Command("copy", "Copy Roslyn to destination directory")]
        public int
            Copy(
                [Option(0, "Destination Directory")] string destination,
                [Option("source", "Source Directory")] string source = TargetDllFolderPath
            )
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                Console.Error.WriteLine("Empty Input!");
                return 1;
            }
            destination = destination.TrimEnd('\\');
            if (!Directory.Exists(source))
            {
                Console.Error.WriteLine("Does not exist " + source);
                return 1;
            }
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }
            Directory.Move(source, destination);
            return 0;
        }
    }
}
