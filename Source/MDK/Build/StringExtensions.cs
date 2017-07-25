using System;
using System.Linq;

namespace MDK.Build
{
    /// <summary>
    /// Utility string extensions
    /// </summary>
    public static class StringExtensions
    {
        const char SingleWildcard = '?';
        const char MultipleWildcard = '*';

        /// <summary>
        /// Changes the given Base-N string into its integer representative
        /// </summary>
        /// <param name="value"></param>
        /// <param name="baseChars"></param>
        /// <returns></returns>
        public static int ToBase10(this string value, char[] baseChars)
        {
            return value.Select(c => Array.IndexOf(baseChars, c)).Aggregate(0, (x, y) => x * baseChars.Length + y);
        }

        /// <summary>
        /// Changes the given integer into a string representing it in Base-N
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="baseChars">The characters the Base-N number system consists of</param>
        /// <returns></returns>
        public static string ToNBaseString(this int value, char[] baseChars)
        {
            // 32 is the worst cast buffer size for base 2 and int.MaxValue
            var i = 32;
            var buffer = new char[i];
            var targetBase = baseChars.Length;

            do
            {
                buffer[--i] = baseChars[value % targetBase];
                value = value / targetBase;
            }
            while (value > 0);

            var result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }

        /// <summary>
        /// Tests whether specified string can be matched agains provided pattern string. Pattern may contain single (?) and multiple (*) replacing
        /// wildcard characters.
        /// </summary>
        /// <param name="input">String which is matched against the pattern.</param>
        /// <param name="pattern">Pattern against which string is matched.</param>
        /// <returns>true if <paramref name="pattern"/> matches the string <paramref name="input"/>; otherwise false.</returns>
        /// <author>Zoran Horvat</author>
        /// <url>http://www.c-sharpcorner.com/uploadfile/b81385/efficient-string-matching-algorithm-with-use-of-wildcard-characters/</url>
        public static bool IsLike(this string input, string pattern)
        {
            input = input.ToUpper();
            pattern = pattern.ToUpper();
            var inputPosStack = new int[(input.Length + 1) * (pattern.Length + 1)]; // Stack containing input positions that should be tested for further matching
            var patternPosStack = new int[inputPosStack.Length]; // Stack containing pattern positions that should be tested for further matching
            var stackPos = -1; // Points to last occupied entry in stack; -1 indicates that stack is empty
            var pointTested = new bool[input.Length + 1, pattern.Length + 1]; // Each true value indicates that input position vs. pattern position has been tested
            var inputPos = 0; // Position in input matched up to the first multiple wildcard in pattern
            var patternPos = 0; // Position in pattern matched up to the first multiple wildcard in pattern
            // Match beginning of the string until first multiple wildcard in pattern
            while (inputPos < input.Length && patternPos < pattern.Length && pattern[patternPos] != MultipleWildcard && (input[inputPos] == pattern[patternPos] || pattern[patternPos] == SingleWildcard))
            {
                inputPos++;
                patternPos++;
            }
            // Push this position to stack if it points to end of pattern or to a general wildcard
            if (patternPos == pattern.Length || pattern[patternPos] == MultipleWildcard)
            {
                pointTested[inputPos, patternPos] = true;
                inputPosStack[++stackPos] = inputPos;
                patternPosStack[stackPos] = patternPos;
            }
            var matched = false;
            // Repeat matching until either string is matched against the pattern or no more parts remain on stack to test
            while (stackPos >= 0 && !matched)
            {
                inputPos = inputPosStack[stackPos]; // Pop input and pattern positions from stack
                patternPos = patternPosStack[stackPos--]; // Matching will succeed if rest of the input string matches rest of the pattern
                if (inputPos == input.Length && patternPos == pattern.Length)
                    matched = true; // Reached end of both pattern and input string, hence matching is successful
                else
                {
                    // First character in next pattern block is guaranteed to be multiple wildcard
                    // So skip it and search for all matches in value string until next multiple wildcard character is reached in pattern
                    for (var curInputStart = inputPos; curInputStart < input.Length; curInputStart++)
                    {
                        var curInputPos = curInputStart;
                        var curPatternPos = patternPos + 1;
                        if (curPatternPos == pattern.Length)
                        {
                            // Pattern ends with multiple wildcard, hence rest of the input string is matched with that character
                            curInputPos = input.Length;
                        }
                        else
                        {
                            while (curInputPos < input.Length && curPatternPos < pattern.Length && pattern[curPatternPos] != MultipleWildcard &&
                                   (input[curInputPos] == pattern[curPatternPos] || pattern[curPatternPos] == SingleWildcard))
                            {
                                curInputPos++;
                                curPatternPos++;
                            }
                        }
                        // If we have reached next multiple wildcard character in pattern without breaking the matching sequence, then we have another candidate for full match
                        // This candidate should be pushed to stack for further processing
                        // At the same time, pair (input position, pattern position) will be marked as tested, so that it will not be pushed to stack later again
                        if (((curPatternPos == pattern.Length && curInputPos == input.Length) || (curPatternPos < pattern.Length && pattern[curPatternPos] == MultipleWildcard))
                            && !pointTested[curInputPos, curPatternPos])
                        {
                            pointTested[curInputPos, curPatternPos] = true;
                            inputPosStack[++stackPos] = curInputPos;
                            patternPosStack[stackPos] = curPatternPos;
                        }
                    }
                }
            }
            return matched;
        }
    }
}
