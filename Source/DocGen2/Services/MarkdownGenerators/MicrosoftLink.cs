using System.Reflection;

namespace Mal.DocGen2.Services.MarkdownGenerators
{
    static class MicrosoftLink
    {
        public static bool IsMsType(MemberInfo memberInfo)
        {
            var assembly = memberInfo.GetAssembly();
            if (assembly.GetName().Name == "mscorlib")
                return true;
            var companyAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            if (companyAttribute?.Company == "Microsoft Corporation")
                return true;
            return false;
        }


    }
}
