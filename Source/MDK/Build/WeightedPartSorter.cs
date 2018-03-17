using System;
using System.Collections.Generic;

namespace MDK.Build
{
    /// <summary>
    /// Compares two script parts against each other, determining their placement order.
    /// </summary>
    public class WeightedPartSorter : IComparer<ScriptPart>
    {
        /// <summary>
        /// Compares two script parts against each other, determining their placement order.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(ScriptPart x, ScriptPart y)
        {
            // If the weights of the given documents differ, this will override the simple alphabetical
            // comparison (Higher weights have higher priority).

            var xWeight = WeightOf(x);
            var yWeight = WeightOf(y);
            var cmp = -xWeight.CompareTo(yWeight);
            if (cmp != 0)
                return cmp;

            return string.Compare(x?.Name, y?.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        long WeightOf(ScriptPart scriptPart)
        {
            if (scriptPart.SortWeight != null)
                return scriptPart.SortWeight.Value;
            long weight = 0;
            if (string.Equals(scriptPart.Name, "Program.cs", StringComparison.CurrentCultureIgnoreCase))
                weight++;
            return weight;
        }
    }
}
