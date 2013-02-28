using System;
using System.Collections.Generic;
using System.Linq;

namespace DnrGps_Wkt
{
    internal class WktToken
    {
        internal WktToken(string s, int startIndex, int endIndex)
        {
            Text = s;
            StartIndex = startIndex;
            EndIndex = endIndex;
            //remove optional whitespace and/or parens at ends of token
            if (IsEmpty)
                return;
            while (Char.IsWhiteSpace(Text[StartIndex]))
                StartIndex++;
            bool removedLeadingParen = false;
            if (Text[StartIndex] == '(')
            {
                StartIndex++;
                removedLeadingParen = true;
            }
            while (Char.IsWhiteSpace(Text[EndIndex]))
                EndIndex--;
            if (Text[EndIndex] == ')' && removedLeadingParen)
                EndIndex--;
        }

        internal string Text { get; private set; }
        internal int StartIndex { get; private set; }
        internal int EndIndex { get; private set; }

        internal bool IsEmpty
        {
            get { return StartIndex < 0 || EndIndex < StartIndex; }
        }

        internal IEnumerable<WktToken> Tokens
        {
            get
            {
                if (IsEmpty)
                    yield break;

                int currentStart = StartIndex;
                //currentStart may be a '(', do not let currentEnd go past it without nesting.
                int currentEnd = StartIndex;
                int nesting = 0;

                while (true)
                {
                    if (currentEnd >= EndIndex)
                    {
                        yield return new WktToken(Text, currentStart, EndIndex);
                        yield break;
                    }

                    if (Text[currentEnd] == '(')
                        nesting++;
                    if (Text[currentEnd] == ')')
                        nesting--;

                    if (nesting == 0 && Text[currentEnd] == ',')
                    {
                        yield return new WktToken(Text, currentStart, currentEnd - 1);
                        currentStart = currentEnd + 1;
                        while (currentStart < EndIndex && Char.IsWhiteSpace(Text[currentStart]))
                            currentStart++;
                        //currentStart may be a '(', do not let currentEnd go past it without nesting.
                        currentEnd = currentStart - 1;
                    }
                    currentEnd++;
                }
            }
        }

        internal IEnumerable<double> Coords
        {
            get
            {
                if (IsEmpty)
                    return new double[0];
                string text = Text.Substring(StartIndex, 1 + EndIndex - StartIndex);
                string[] words = text.Split((Char[])null, StringSplitOptions.RemoveEmptyEntries);
                return words.Select(Convert.ToDouble);
            }
        }

        public override string ToString()
        {
            return Text.Substring(StartIndex, 1 + EndIndex - StartIndex);
        }
    }
}
