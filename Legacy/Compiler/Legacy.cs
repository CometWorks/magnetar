#if NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using Pulsar.Compiler;

namespace Pulsar.Legacy.Compiler;

internal class CompilerFactory(string[] probeDirs, string gameDir, string logDir) : ICompilerFactory
{
    private AppDomain appDomain = null;

    public void Init()
    {
        string[] refererences = [.. References.GetReferences(gameDir)];
        appDomain = CreateAppDomain(refererences, probeDirs, logDir);
    }

    public ICompiler Create(bool debugBuild = false)
    {
        if (appDomain is null)
            Init();

        RoslynCompiler instance = (RoslynCompiler)
            appDomain.CreateInstanceAndUnwrap(
                typeof(RoslynCompiler).Assembly.FullName,
                typeof(RoslynCompiler).FullName
            );

        instance.DebugBuild = debugBuild;
        // NETFRAMEWORK only ever runs on Windows, so the platform is fixed here.
        // Running on Proton/Wine is considered running on Windows, so no difference.
        instance.Flags = debugBuild
            ? ["NETFRAMEWORK", "PLATFORM_WINDOWS", "TRACE", "DEBUG"]
            : ["NETFRAMEWORK", "PLATFORM_WINDOWS", "TRACE"];

        return instance;
    }

    private static void SetupAppDomain()
    {
        var assemblies = (string[])AppDomain.CurrentDomain.GetData("assemblies");
        var probeDirs = (string[])AppDomain.CurrentDomain.GetData("probeDirs");
        var logDir = (string)AppDomain.CurrentDomain.GetData("logDir");

        Pulsar.Compiler.LogFile.Init(logDir);

        foreach (string dir in probeDirs)
            RoslynReferences.Instance.Resolver.AddSearchDirectory(dir);

        RoslynReferences.Instance.GenerateAssemblyList(assemblies);
    }

    private AppDomain CreateAppDomain(string[] assemblies, string[] probeDirs, string logDir)
    {
        string applicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        string libraryDir = Path.Combine("Libraries", Assembly.GetExecutingAssembly().GetName().Name);
        string compilerDir = Path.Combine(libraryDir, "Compiler");

        // deploy.bat keeps Magnetar.Compiler and shared dependencies in the
        // launcher library root, while Roslyn-private dependencies live under
        // Compiler. The child AppDomain must probe both.
        string privateBinPath = string.Join(Path.PathSeparator.ToString(), libraryDir, compilerDir);

        string configurationFile = Path.Combine(libraryDir, "Magnetar.Compiler.dll.config");

        AppDomainSetup current = AppDomain.CurrentDomain.SetupInformation;
        AppDomainSetup config = new()
        {
            ApplicationBase = applicationBase,
            PrivateBinPath = privateBinPath,
            ConfigurationFile = configurationFile,
        };

        AppDomain domain = AppDomain.CreateDomain("Pulsar.Compiler", null, config);

        domain.SetData("probeDirs", probeDirs);
        domain.SetData("logDir", logDir);
        domain.SetData("assemblies", assemblies);
        domain.DoCallBack(SetupAppDomain);

        return domain;
    }

    public void Dispose()
    {
        if (appDomain is not null)
            AppDomain.Unload(appDomain);
    }
}
#endif
