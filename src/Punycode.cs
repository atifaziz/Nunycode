#region Copyright (C) 2017 Atif Aziz. Portions Copyright Mathias Bynens.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

[assembly: System.CLSCompliant(true)]

namespace Nunycode
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class Ucs2
    {
        public static int[] Decode(string @string) => Punycode.Ucs2Decode(@string);
        public static string Encode(params int[] codePoints) => Punycode.Ucs2Encode(codePoints);
    }

    public static class Punycode
    {
        const int MaxInt = int.MaxValue; // aka. 0x7FFFFFFF or 2^31-1

        // Bootstring parameters

        const int Base = 36;
        const int TMin = 1;
        const int TMax = 26;
        const int Skew = 38;
        const int Damp = 700;
        const int InitialBias = 72;
        const int InitialN = 128; // 0x80
        const char Delimiter = '-'; // '\x2D'

        // Regular expressions

        static readonly Regex RegexNonAscii = new Regex(@"[^\0-\x7E]", RegexOptions.ECMAScript); // non-ASCII chars
        static readonly char[] Rfc3490Separators = { '\x2E', '\u3002', '\uFF0E', '\uFF61' };

        // Convenience shortcuts

        const int BaseMinusTMin = Base - TMin;

        /*--------------------------------------------------------------------------*/

        static ArgumentOutOfRangeException OverflowError() =>
            RangeError("Overflow: input needs wider integers to process");
        static ArgumentOutOfRangeException InvalidInputError() =>
            RangeError("Invalid input");
        static ArgumentOutOfRangeException NotBasicError() =>
            RangeError("Illegal input >= 0x80 (not a basic code point)");

        static ArgumentOutOfRangeException RangeError(string message) =>
            new ArgumentOutOfRangeException(message);


        /// <summary>
        /// A simple array map-like wrapper to work with domain name strings
        /// or email addresses.
        /// </summary>

        static string MapDomain(string @string, Func<string, string> fn)
        {
            var parts = @string.Split('@');
            var result = new StringBuilder();
            if (parts.Length > 1)
            {
                // In email addresses, only the domain name should be punycoded. Leave
                // the local part (i.e. everything up to `@`) intact.
                result.Append(parts[0] + "@");
                @string = parts[1];
            }
            var labels = @string.Split(Rfc3490Separators);
            var encoded = string.Join(".", labels.Select(fn));
            return result.Append(encoded).ToString();
        }

        /// <summary>
        /// Creates an array containing the numeric code points of each Unicode
        /// character in the string. While JavaScript uses UCS-2 internally,
        /// this function will convert a pair of surrogate halves (each of which
        /// UCS-2 exposes as separate characters) into a single code point,
        /// matching UTF-16.
        /// </summary>
        /// <remarks>
        /// See also
        /// <a href="https://mathiasbynens.be/notes/javascript-encoding">JavaScript's
        /// internal character encoding: UCS-2 or UTF-16?</a>.
        /// </remarks>

        internal static int[] Ucs2Decode(string @string)
        {
            var output = (default(int[]), 0);
            var counter = 0;
            var length = @string.Length;
            while (counter < length)
            {
                var value = @string.CharCodeAt(counter++);
                if (value >= 0xD800 && value <= 0xDBFF && counter < length)
                {
                    // It's a high surrogate, and there is a next character.
                    var extra = @string.CharCodeAt(counter++);
                    if ((extra & 0xFC00) == 0xDC00)
                    { // Low surrogate.
                        output = output.Push(((value & 0x3FF) << 10) + (extra & 0x3FF) + 0x10000);
                    }
                    else
                    {
                        // It's an unmatched surrogate; only append this code unit, in case the
                        // next code unit is the high surrogate of a surrogate pair.
                        output = output.Push(value);
                        counter--;
                    }
                }
                else
                {
                    output = output.Push(value);
                }
            }
            return output.ToArray();
        }

        /// <summary>
        /// Creates a string based on an array of numeric code points.
        /// </summary>

        internal static string Ucs2Encode(params int[] codePoints)
        {
            const int maxSize = 0x4000;
            var codeUnits = (Buffer: default(char[]), Length: 0);
            var index = -1;
            var length = codePoints.Length;
            if (length == 0)
            {
                return string.Empty;
            }
            var result = new StringBuilder();
            while (++index < length)
            {
                var codePoint = codePoints[index];
                if (
                    codePoint < 0 ||              // not a valid Unicode code point
                    codePoint > 0x10FFFF          // not a valid Unicode code point
                )
                {
                    throw new ArgumentOutOfRangeException("Invalid code point: " + codePoint);
                }
                if (codePoint <= 0xFFFF)
                { // BMP code point
                    codeUnits = codeUnits.Push((char)codePoint);
                }
                else
                { // Astral code point; split in surrogate halves
                    // http://mathiasbynens.be/notes/javascript-encoding#surrogate-formulae
                    codePoint -= 0x10000;
                    var highSurrogate = (codePoint >> 10) + 0xD800;
                    var lowSurrogate = (codePoint % 0x400) + 0xDC00;
                    codeUnits = codeUnits.Push((char)highSurrogate, (char)lowSurrogate);
                }
                if (index + 1 == length || codeUnits.Length > maxSize)
                {
                    result.Append(codeUnits.Buffer, 0, codeUnits.Length);
                    codeUnits = (codeUnits.Buffer, 0);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Converts a basic code point into a digit/integer.
        /// </summary>
        /// <returns>
        /// The numeric value of a basic code point (for use in
        /// representing integers) in the range <c>0</c> to <c>base - 1</c>,
        /// or <base>base</base> if the code point does not represent a value.
        /// </returns>

        static int BasicToDigit(int codePoint)
            => codePoint - 0x30 < 0x0A ? codePoint - 0x16
             : codePoint - 0x41 < 0x1A ? codePoint - 0x41
             : codePoint - 0x61 < 0x1A ? codePoint - 0x61
             : Base;

        /// <summary>
        /// Converts a digit/integer into a basic code point.
        /// </summary>
        /// <returns>
        ///  The basic code point whose value (when used for
        /// representing integers) is <c>digit</c>, which needs to be in the range
        /// <c>0</c> to <c>base - 1</c>. If <c>flag</c> is non-zero, the uppercase form is
        /// used; else, the lowercase form is used. The behavior is undefined
        /// if <c>flag</c> is non-zero and <c>digit</c> has no uppercase form.
        /// </returns>

        static int DigitToBasic(int digit, bool flag) =>
            //  0..25 map to ASCII a..z or A..Z
            // 26..35 map to ASCII 0..9
            digit + 22 + 75 * (digit < 26 ? 1 : 0) - ((flag ? 1 : 0) << 5);

        /// <summary>
        /// Bias adaptation function as per section 3.4 of RFC 3492.
        /// </summary>
        /// <remarks>
        /// See <a href="https://tools.ietf.org/html/rfc3492#section-3.4">3.4
        /// Bias adaptation</a>.
        /// </remarks>

        static int Adapt(int delta, int numPoints, bool firstTime)
        {
            var k = 0;
            delta = firstTime ? delta / Damp : delta >> 1;
            delta += delta / numPoints;
            for (/* no initialization */; delta > BaseMinusTMin * TMax >> 1; k += Base)
                delta = delta / BaseMinusTMin;
            return k + (BaseMinusTMin + 1) * delta / (delta + Skew);
        }

        /// <summary>
        /// Converts a Punycode string of ASCII-only symbols to a string of Unicode
        /// symbols.
        /// </summary>

        public static string Decode(string input)
        {
            // Don't use UCS-2.
            var output = (Buffer: default(int[]), Length: 0);
            var inputLength = input.Length;
            var i = 0;
            var n = InitialN;
            var bias = InitialBias;

            // Handle the basic code points: let `basic` be the number of input code
            // points before the last delimiter, or `0` if there is none, then copy
            // the first basic code points to the output.

            var basic = input.LastIndexOf(Delimiter);
            if (basic < 0)
            {
                basic = 0;
            }

            for (var j = 0; j < basic; ++j)
            {
                // if it's not a basic code point
                if (input.CharCodeAt(j) >= 0x80)
                {
                    throw NotBasicError();
                }
                output = output.Push(input.CharCodeAt(j));
            }

            // Main decoding loop: start just after the last delimiter if any basic code
            // points were copied; start at the beginning otherwise.

            for (var index = basic > 0 ? basic + 1 : 0; index < inputLength; /* no final expression */)
            {

                // `index` is the index of the next character to be consumed.
                // Decode a generalized variable-length integer into `delta`,
                // which gets added to `i`. The overflow checking is easier
                // if we increase `i` as we go, then subtract off its starting
                // value at the end to obtain `delta`.
                var oldi = i;
                var w = 1;
                for (var k = Base; /* no condition */; k += Base)
                {

                    if (index >= inputLength)
                    {
                        throw InvalidInputError();
                    }

                    var digit = BasicToDigit(input.CharCodeAt(index++));

                    if (digit >= Base || digit > (MaxInt - i) / w)
                    {
                        throw OverflowError();
                    }

                    i += digit * w;
                    var t = k <= bias ? TMin : (k >= bias + TMax ? TMax : k - bias);

                    if (digit < t)
                    {
                        break;
                    }

                    var baseMinusT = Base - t;
                    if (w > MaxInt / baseMinusT)
                    {
                        throw OverflowError();
                    }

                    w *= baseMinusT;

                }

                var @out = output.Length + 1;
                bias = Adapt(i - oldi, @out, oldi == 0);

                // `i` was supposed to wrap around from `out` to `0`,
                // incrementing `n` each time, so we'll fix that now:
                if (i / @out > MaxInt - n)
                {
                    throw OverflowError();
                }

                n += i / @out;
                i %= @out;

                // Insert `n` at position `i` of the output.
                output = output.Splice(i++, 0, n);

            }

            var bytes = (Buffer: default(byte[]), Length: 0);
            var na = new byte[sizeof(int)];
            for (var oi = 0; oi < output.Length; oi++)
            {
                output.Buffer[oi].ToBytes(na, 0);
                bytes = bytes.Push(na);
            }
            return Encoding.UTF32.GetString(bytes.Buffer, 0, bytes.Length);
        }

        /// <summary>
        /// Converts a string of Unicode symbols (e.g. a domain name label) to a
        /// Punycode string of ASCII-only symbols.
        /// </summary>

        public static string Encode(string input)
        {
            var output = new StringBuilder();

            // Convert the input in UCS-2 to an array of Unicode code points.
            var inpu1 = Ucs2Decode(input);

            // Cache the length.
            var inputLength = inpu1.Length;

            // Initialize the state.
            var n = InitialN;
            var delta = 0;
            var bias = InitialBias;

            // Handle the basic code points.
            foreach (var currentValue in inpu1)
            {
                if (currentValue < 0x80)
                {
                    output.Push((char) currentValue);
                }
            }

            var basicLength = output.Length;
            // ReSharper disable once InconsistentNaming
            var handledCPCount = basicLength;

            // `handledCPCount` is the number of code points that have been handled;
            // `basicLength` is the number of basic code points.

            // Finish the basic string with a delimiter unless it's empty.
            if (basicLength > 0)
            {
                output.Push(Delimiter);
            }

            // Main encoding loop:
            while (handledCPCount < inputLength)
            {

                // All non-basic code points < n have been handled already. Find the next
                // larger one:
                var m = MaxInt;
                foreach (var currentValue in inpu1)
                {
                    if (currentValue >= n && currentValue < m)
                    {
                        m = currentValue;
                    }
                }

                // Increase `delta` enough to advance the decoder's <n,i> state to <m,0>,
                // but guard against overflow.
                // ReSharper disable once InconsistentNaming
                var handledCPCountPlusOne = handledCPCount + 1;
                if (m - n > (MaxInt - delta) / handledCPCountPlusOne)
                {
                    throw OverflowError();
                }

                delta += (m - n) * handledCPCountPlusOne;
                n = m;

                foreach (var currentValue in inpu1)
                {
                    if (currentValue < n && ++delta > MaxInt)
                    {
                        throw OverflowError();
                    }
                    if (currentValue == n)
                    {
                        // Represent delta as a generalized variable-length integer.
                        var q = delta;
                        for (var k = Base; /* no condition */; k += Base)
                        {
                            var t = k <= bias ? TMin : (k >= bias + TMax ? TMax : k - bias);
                            if (q < t)
                            {
                                break;
                            }
                            var qMinusT = q - t;
                            var baseMinusT = Base - t;
                            output.Push(
                                    (char) DigitToBasic(t + qMinusT % baseMinusT, false)
                            );
                            q = qMinusT / baseMinusT;
                        }

                        output.Push((char) DigitToBasic(q, false));
                        bias = Adapt(delta, handledCPCountPlusOne, handledCPCount == basicLength);
                        delta = 0;
                        ++handledCPCount;
                    }
                }

                ++delta;
                ++n;

            }
            return output.ToString();
        }

        /// <summary>
        /// Converts a Punycode string representing a domain name or an email address
        /// to Unicode. Only the Punycoded parts of the input will be converted, i.e.
        /// it doesn't matter if you call it on a string that has already been
        /// converted to Unicode.
        /// </summary>

        public static string ToUnicode(string input) =>
            MapDomain(input, @string =>
                @string.StartsWith("xn--", StringComparison.Ordinal)
                    ? Decode(@string.Substring(4).ToLowerInvariant())
                    : @string);

        /// <summary>
        /// Converts a Unicode string representing a domain name or an email address to
        /// Punycode. Only the non-ASCII parts of the domain name will be converted,
        /// i.e. it doesn't matter if you call it with a domain that's already in
        /// ASCII.
        /// </summary>

        public static string ToAscii(string input) =>
            MapDomain(input, @string =>
                RegexNonAscii.IsMatch(@string)
                    ? "xn--" + Encode(@string)
                    : @string);
    }
}
