using System.Runtime.CompilerServices;
using VerifyTests;

namespace DomainBase.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}