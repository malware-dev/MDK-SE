using System.Reflection;

namespace Mal.DocGen2.Services
{
    class MemberRule : WhitelistRule
    {
        public MemberInfo MemberInfo { get; }

        public MemberRule(MemberInfo memberInfo) : base(memberInfo.GetAssembly())
        {
            MemberInfo = memberInfo;
        }

        public override bool IsMatch(MemberInfo memberInfo)
        {
            return memberInfo == MemberInfo;
        }
    }
}