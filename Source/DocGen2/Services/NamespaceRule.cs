using System.Reflection;

namespace Mal.DocGen2.Services
{
    class NamespaceRule : WhitelistRule
    {
        public string NamespaceName { get; }

        public NamespaceRule(string namespaceName, Assembly assembly): base(assembly)
        {
            NamespaceName = namespaceName;
        }

        public override bool IsMatch(MemberInfo memberInfo)
        {
            return memberInfo.GetAssembly() == Assembly && memberInfo.GetNamespace() == NamespaceName;
        }
    }
}