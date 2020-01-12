using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mal.DocGen2.Services
{
    class TypeRule : WhitelistRule
    {
        public Type Type { get; }
        public bool AllMembers { get; }

        public TypeRule(Type type, bool allMembers): base(type.Assembly)
        {
            Type = type;
            AllMembers = allMembers;
        }

        public override bool IsMatch(MemberInfo memberInfo)
        {
            if (!AllMembers)
                return memberInfo == Type;

            if (memberInfo is Type type && type == Type)
                return true;

            return NestsAndSelfOf(memberInfo.DeclaringType).Any(t => t == Type);
        }

        static IEnumerable<Type> NestsAndSelfOf(Type type)
        {
            if (type == null)
                yield break;
            while (type != null)
            {
                yield return type;
                type = type.DeclaringType;
            }
        }
    }
}