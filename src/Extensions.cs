#region Copyright (C) 2017 Atif Aziz.
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

namespace Nunycode
{
    using System;
    using System.Text;

    static class Extensions
    {
        public static void ToBytes(this int value, byte[] array, int offset)
        {
            for (var i = 0; i < sizeof(int); i++)
            {
                array[offset + i] = (byte)value;
                value >>= 8;
            }
        }

        public static StringBuilder Push(this StringBuilder sb, char ch) => sb.Append(ch);

        public static int CharCodeAt(this string s, int i) => s[i];

        public static (T[] Buffer, int Length) Push<T>(this (T[] Buffer, int Length) array, params T[] values) =>
            array.Splice(array.Length, 0, values);

        public static (T[] Buffer, int Length) Splice<T>(this (T[], int) array, int index, int deletions, params T[] insertions)
        {
            var (buffer, length) = array;
            var size = buffer?.Length ?? 0;
            var newSize = length + insertions.Length - deletions;
            if (newSize > size)
                Array.Resize(ref buffer, size == 0 ? Math.Max(4, newSize) : Math.Max(size * 2, newSize));
            Array.Copy(buffer, index + deletions, buffer, index + insertions.Length, length - index - deletions);
            Array.Copy(insertions, 0, buffer, index, insertions.Length);
            return (buffer, length - deletions + insertions.Length);
        }

        public static T[] ToArray<T>(this (T[], int) array)
        {
            var (buffer, length) = array;
            Array.Resize(ref buffer, length);
            return buffer;
        }
    }
}