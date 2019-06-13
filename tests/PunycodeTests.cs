#region Copyright (C) 2017 Atif Aziz
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

namespace Nunycode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class PunycodeTests
    {
        #region UCS2

        //  The Ucs2Decode and Ucs2Encode tests require some special treatment
        //  because passing decoded and encoded data as arguments crashes
        //  the test environment due to the following error:
        //
        //  System.Text.EncoderFallbackException: Unable to translate Unicode character ... at index ... to specified code page.
        //     at System.Text.EncoderExceptionFallbackBuffer.Fallback(Char charUnknown, Int32 index)
        //     at System.Text.EncoderFallbackBuffer.InternalFallback(Char ch, Char*& chars)
        //     at System.Text.UTF8Encoding.GetByteCount(Char* chars, Int32 count, EncoderNLS baseEncoder)
        //     at System.Text.UTF8Encoding.GetByteCount(String chars)
        //     at System.IO.BinaryWriter.Write(String value)
        //     at Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketCommunicationManager.WriteAndFlushToChannel(String rawMessage)
        //     at Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution.TestRunCache.SendResults()
        //     at Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution.TestRunCache.CheckForCacheHit()
        //     at Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution.TestRunCache.OnNewTestResult(TestResult testResult)
        //     at Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter.TestExecutionRecorder.RecordResult(TestResult testResult)
        //     at Xunit.Runner.VisualStudio.TestAdapter.VsExecutionSink.TryAndReport(String actionDescription, ITestCase testCase, Action action)
        //
        //  The workaround is to use the description to look-up the associated
        //  coded and encoded data to be used for the assertion.
        //

        [Theory, MemberData(nameof(Ucs2))]
        public void Ucs2Decode(string description) =>
            Ucs2Codec(description, (decoded, encoded) =>
                Assert.Equal(decoded, Nunycode.Ucs2.Decode(encoded)));

        [Theory, MemberData(nameof(Ucs2))]
        public void Ucs2Encode(string description) =>
            Ucs2Codec(description, (decoded, encoded) =>
                Assert.Equal(encoded, Nunycode.Ucs2.Encode(decoded)));

        static void Ucs2Codec(string description, Action<int[], string> asserter)
        {
            var data = GetUcs2Data((desc, decoded, encoded) => new { Description = desc, Decoded = decoded, Encoded = encoded });
            var t = data.Single(e => e.Description == description);
            asserter(t.Decoded, t.Encoded);
        }

        public static IEnumerable<object[]> Ucs2 =>
            GetUcs2Data((desc, decoded, encoded) => new object[] { desc });

        public static IEnumerable<T> GetUcs2Data<T>(Func<string, int[], string, T> selector) =>
            from t in new[]
            {
                // Every Unicode symbol is tested separately. These are just the extra
                // tests for symbol combinations:
                new
                {
                    Description = "Consecutive astral symbols",
                    Decoded = new[] { 127829, 119808, 119558, 119638 },
                    Encoded = "\uD83C\uDF55\uD835\uDC00\uD834\uDF06\uD834\uDF56"
                },
                new
                {
                    Description = "U+D800 (high surrogate) followed by non-surrogates",
                    Decoded = new[] { 55296, 97, 98 },
                    Encoded = "\uD800ab"
                },
                new
                {
                    Description = "U+DC00 (low surrogate) followed by non-surrogates",
                    Decoded = new[] { 56320, 97, 98 },
                    Encoded = "\uDC00ab"
                },
                new
                {
                    Description = "High surrogate followed by another high surrogate",
                    Decoded = new[] { 0xD800, 0xD800 },
                    Encoded = "\uD800\uD800"
                },
                new
                {
                    Description = "Unmatched high surrogate, followed by a surrogate pair, followed by an unmatched high surrogate",
                    Decoded = new[] { 0xD800, 0x1D306, 0xD800 },
                    Encoded = "\uD800\uD834\uDF06\uD800"
                },
                new
                {
                    Description = "Low surrogate followed by another low surrogate",
                    Decoded = new[] { 0xDC00, 0xDC00 },
                    Encoded = "\uDC00\uDC00"
                },
                new
                {
                    Description = "Unmatched low surrogate, followed by a surrogate pair, followed by an unmatched low surrogate",
                    Decoded = new[] { 0xDC00, 0x1D306, 0xDC00 },
                    Encoded = "\uDC00\uD834\uDF06\uDC00"
                }
            }
            select selector(t.Description, t.Decoded, t.Encoded);

        #endregion

        [Fact(DisplayName = "Illegal input >= 0x80 (not a basic code point)")]
        void DecodeFailsOnIllegalInput() =>
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Punycode.Decode("\x81-"));

        [Fact(DisplayName = "Overflow: input needs wider integers to process")]
        void DecodeFailsOnOverflow() =>
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Punycode.Decode("\x81"));

        #pragma warning disable xUnit1026

        // warning xUnit1026: Theory method 'Decode' on test class 'PunycodeTests' does not use parameter 'description'.
        // warning xUnit1026: Theory method 'Encode' on test class 'PunycodeTests' does not use parameter 'description'.
        // warning xUnit1026: Theory method 'UnicodeDomains' on test class 'PunycodeTests' does not use parameter 'description'.
        // warning xUnit1026: Theory method 'UnicodeStrings' on test class 'PunycodeTests' does not use parameter 'description'.
        // warning xUnit1026: Theory method 'AsciiDomains' on test class 'PunycodeTests' does not use parameter 'description'.
        // warning xUnit1026: Theory method 'AsciiStrings' on test class 'PunycodeTests' does not use parameter 'description'.
        // warning xUnit1026: Theory method 'AsciiStrings' on test class 'PunycodeTests' does not use parameter 'decoded'.
        // warning xUnit1026: Theory method 'AsciiSeparators' on test class 'PunycodeTests' does not use parameter 'description'.

        [Theory, MemberData(nameof(Strings), DescriptionFallback.Encoded)]
        public void Decode(string description, string decoded, string encoded) =>
            Assert.Equal(decoded, Punycode.Decode(encoded));

        [Theory, MemberData(nameof(Strings), DescriptionFallback.Decoded)]
        public void Encode(string description, string decoded, string encoded) =>
            Assert.Equal(encoded, Punycode.Encode(decoded));

        [Theory, MemberData(nameof(Domains), DescriptionFallback.Encoded)]
        public void UnicodeDomains(string description, string decoded, string encoded) =>
            Assert.Equal(decoded, Punycode.ToUnicode(encoded));

        [Theory(DisplayName = "does not convert names (or other strings) that don\'t start with `xn--`")]
        [MemberData(nameof(Strings), DescriptionFallback.None)]
        public void UnicodeStrings(string description, string decoded, string encoded)
        {
            Assert.Equal(encoded, Punycode.ToUnicode(encoded));
            Assert.Equal(decoded, Punycode.ToUnicode(decoded));
        }

        [Theory, MemberData(nameof(Domains), DescriptionFallback.Decoded)]
        public void AsciiDomains(string description, string decoded, string encoded) =>
            Assert.Equal(encoded, Punycode.ToAscii(decoded));

        [Theory(DisplayName = "does not convert domain names (or other strings) that are already in ASCII")]
        [MemberData(nameof(Strings), DescriptionFallback.None)]
        public void AsciiStrings(string description, string decoded, string encoded) =>
            Assert.Equal(encoded, Punycode.ToAscii(encoded));

        [Theory(DisplayName = "supports IDNA2003 separators for backwards compatibility")]
        [MemberData(nameof(Separators))]
        public void AsciiSeparators(string description, string decoded, string encoded) =>
            Assert.Equal(encoded, Punycode.ToAscii(decoded));

        #pragma warning restore xUnit1026

        public static IEnumerable<object[]> Domains(DescriptionFallback fallback) =>
            from t in new[]
            {
                new
                {
                    Description = default(string),
                    Decoded = "ma\xF1" + "ana.com",
                    Encoded = "xn--maana-pta.com"
                },
                new
                { // https://github.com/bestiejs/punycode.js/issues/17
                    Description = default(string),
                    Decoded = "example.com.",
                    Encoded = "example.com."
                },
                new
                {
                    Description = default(string),
                    Decoded = "b\xFC" + "cher.com",
                    Encoded = "xn--bcher-kva.com"
                },
                new
                {
                    Description = default(string),
                    Decoded = "caf\xE9.com",
                    Encoded = "xn--caf-dma.com"
                },
                new
                {
                    Description = default(string),
                    Decoded = "\u2603-\u2318.com",
                    Encoded = "xn----dqo34k.com"
                },
                new
                {
                    Description = default(string),
                    Decoded = "\uD400\u2603-\u2318.com",
                    Encoded = "xn----dqo34kn65z.com"
                },
                new
                {
                    Description = "Emoji",
                    Decoded = "\uD83D\uDCA9.la",
                    Encoded = "xn--ls8h.la"
                },
                new
                {
                    Description = "Non-printable ASCII",
                    Decoded = "\0\x01\x02" + "foo.bar",
                    Encoded = "\0\x01\x02" + "foo.bar"
                },
                new
                {
                    Description = "Email address",
                    Decoded = "\u0434\u0436\u0443\u043C\u043B\u0430@\u0434\u0436p\u0443\u043C\u043B\u0430\u0442\u0435\u0441\u0442.b\u0440\u0444a",
                    Encoded = "\u0434\u0436\u0443\u043C\u043B\u0430@xn--p-8sbkgc5ag7bhce.xn--ba-lmcq"
                }
            }
            select new object[]
            {
                Fallback(t.Description, fallback, t.Decoded, t.Encoded),
                t.Decoded,
                t.Encoded
            };

        public static IEnumerable<object[]> Separators =>
            from t in new[]
            {
                new
                {
                    Description = "Using U+002E as separator",
                    Decoded = "ma\xF1" + "ana\x2E" + "com",
                    Encoded = "xn--maana-pta.com"
                },
                new
                {
                    Description = "Using U+3002 as separator",
                    Decoded = "ma\xF1" + "ana\u3002com",
                    Encoded = "xn--maana-pta.com"
                },
                new
                {
                    Description = "Using U+FF0E as separator",
                    Decoded = "ma\xF1" + "ana\uFF0Ecom",
                    Encoded = "xn--maana-pta.com"
                },
                new
                {
                    Description = "Using U+FF61 as separator",
                    Decoded = "ma\xF1" + "ana\uFF61com",
                    Encoded = "xn--maana-pta.com"
                }
            }
            select new object[] { t.Description, t.Decoded, t.Encoded };

        public enum DescriptionFallback { None, Decoded, Encoded }

        public static IEnumerable<object[]> Strings(DescriptionFallback fallback) =>
            from t in new[]
            {
                new
                {
                    Description = "a single basic code point",
                    Decoded = "Bach",
                    Encoded = "Bach-"
                },
                new
                {
                    Description = "a single non-ASCII character",
                    Decoded = "\xFC",
                    Encoded = "tda"
                },
                new
                {
                    Description = "multiple non-ASCII characters",
                    Decoded = "\xFC\xEB\xE4\xF6\u2665",
                    Encoded = "4can8av2009b"
                },
                new
                {
                    Description = "mix of ASCII and non-ASCII characters",
                    Decoded = "b\xFC" + "cher",
                    Encoded = "bcher-kva"
                },
                new
                {
                    Description = "long string with both ASCII and non-ASCII characters",
                    Decoded = "Willst du die Bl\xFCthe des fr\xFChen, die Fr\xFC" + "chte des sp\xE4teren Jahres",
                    Encoded = "Willst du die Blthe des frhen, die Frchte des spteren Jahres-x9e96lkal"
                },
                // https://tools.ietf.org/html/rfc3492#section-7.1
                new
                {
                    Description = "Arabic (Egyptian)",
                    Decoded = "\u0644\u064A\u0647\u0645\u0627\u0628\u062A\u0643\u0644\u0645\u0648\u0634\u0639\u0631\u0628\u064A\u061F",
                    Encoded = "egbpdaj6bu4bxfgehfvwxn"
                },
                new
                {
                    Description = "Chinese (simplified)",
                    Decoded = "\u4ED6\u4EEC\u4E3A\u4EC0\u4E48\u4E0D\u8BF4\u4E2d\u6587",
                    Encoded = "ihqwcrb4cv8a8dqg056pqjye"
                },
                new
                {
                    Description = "Chinese (traditional)",
                    Decoded = "\u4ED6\u5011\u7232\u4EC0\u9EBD\u4E0D\u8AAA\u4E2D\u6587",
                    Encoded = "ihqwctvzc91f659drss3x8bo0yb"
                },
                new
                {
                    Description = "Czech",
                    Decoded = "Pro\u010Dprost\u011Bnemluv\xED\u010Desky",
                    Encoded = "Proprostnemluvesky-uyb24dma41a"
                },
                new
                {
                    Description = "Hebrew",
                    Decoded = "\u05DC\u05DE\u05D4\u05D4\u05DD\u05E4\u05E9\u05D5\u05D8\u05DC\u05D0\u05DE\u05D3\u05D1\u05E8\u05D9\u05DD\u05E2\u05D1\u05E8\u05D9\u05EA",
                    Encoded = "4dbcagdahymbxekheh6e0a7fei0b"
                },
                new
                {
                    Description = "Hindi (Devanagari)",
                    Decoded = "\u092F\u0939\u0932\u094B\u0917\u0939\u093F\u0928\u094D\u0926\u0940\u0915\u094D\u092F\u094B\u0902\u0928\u0939\u0940\u0902\u092C\u094B\u0932\u0938\u0915\u0924\u0947\u0939\u0948\u0902",
                    Encoded = "i1baa7eci9glrd9b2ae1bj0hfcgg6iyaf8o0a1dig0cd"
                },
                new
                {
                    Description = "Japanese (kanji and hiragana)",
                    Decoded = "\u306A\u305C\u307F\u3093\u306A\u65E5\u672C\u8A9E\u3092\u8A71\u3057\u3066\u304F\u308C\u306A\u3044\u306E\u304B",
                    Encoded = "n8jok5ay5dzabd5bym9f0cm5685rrjetr6pdxa"
                },
                new
                {
                    Description = "Korean (Hangul syllables)",
                    Decoded = "\uC138\uACC4\uC758\uBAA8\uB4E0\uC0AC\uB78C\uB4E4\uC774\uD55C\uAD6D\uC5B4\uB97C\uC774\uD574\uD55C\uB2E4\uBA74\uC5BC\uB9C8\uB098\uC88B\uC744\uAE4C",
                    Encoded = "989aomsvi5e83db1d2a355cv1e0vak1dwrv93d5xbh15a0dt30a5jpsd879ccm6fea98c"
                },
                /**
                 * As there's no way to do it in JavaScript, Punycode.js doesn't support
                 * mixed-case annotation (which is entirely optional as per the RFC).
                 * So, while the RFC sample string encodes to:
                 * `b1abfaaepdrnnbgefbaDotcwatmq2g4l`
                 * Without mixed-case annotation it has to encode to:
                 * `b1abfaaepdrnnbgefbadotcwatmq2g4l`
                 * https://github.com/bestiejs/punycode.js/issues/3
                 */
                new
                {
                    Description = "Russian (Cyrillic)",
                    Decoded = "\u043F\u043E\u0447\u0435\u043C\u0443\u0436\u0435\u043E\u043D\u0438\u043D\u0435\u0433\u043E\u0432\u043E\u0440\u044F\u0442\u043F\u043E\u0440\u0443\u0441\u0441\u043A\u0438",
                    Encoded = "b1abfaaepdrnnbgefbadotcwatmq2g4l"
                },
                new
                {
                    Description = "Spanish",
                    Decoded = "Porqu\xE9nopuedensimplementehablarenEspa\xF1ol",
                    Encoded = "PorqunopuedensimplementehablarenEspaol-fmd56a"
                },
                new
                {
                    Description = "Vietnamese",
                    Decoded = "T\u1EA1isaoh\u1ECDkh\xF4ngth\u1EC3ch\u1EC9n\xF3iti\u1EBFngVi\u1EC7t",
                    Encoded = "TisaohkhngthchnitingVit-kjcr8268qyxafd2f1b9g"
                },
                new
                {
                    Description = default(string),
                    Decoded = "3\u5E74B\u7D44\u91D1\u516B\u5148\u751F",
                    Encoded = "3B-ww4c5e180e575a65lsy2b"
                },
                new
                {
                    Description = default(string),
                    Decoded = "\u5B89\u5BA4\u5948\u7F8E\u6075-with-SUPER-MONKEYS",
                    Encoded = "-with-SUPER-MONKEYS-pc58ag80a8qai00g7n9n"
                },
                new
                {
                    Description = default(string),
                    Decoded = "Hello-Another-Way-\u305D\u308C\u305E\u308C\u306E\u5834\u6240",
                    Encoded = "Hello-Another-Way--fc4qua05auwb3674vfr0b"
                },
                new
                {
                    Description = default(string),
                    Decoded = "\u3072\u3068\u3064\u5C4B\u6839\u306E\u4E0B2",
                    Encoded = "2-u9tlzr9756bt3uc0v"
                },
                new
                {
                    Description = default(string),
                    Decoded = "Maji\u3067Koi\u3059\u308B5\u79D2\u524D",
                    Encoded = "MajiKoi5-783gue6qz075azm5e"
                },
                new
                {
                    Description = default(string),
                    Decoded = "\u30D1\u30D5\u30A3\u30FCde\u30EB\u30F3\u30D0",
                    Encoded = "de-jg4avhby1noc0d"
                },
                new
                {
                    Description = default(string),
                    Decoded = "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067",
                    Encoded = "d9juau41awczczp"
                },
                /**
                 * This example is an ASCII string that breaks the existing rules for host
                 * name labels. (It's not a realistic example for IDNA, because IDNA never
                 * encodes pure ASCII labels.)
                 */
                new
                {
                    Description = "ASCII string that breaks the existing rules for host-name labels",
                    Decoded = "-> $1.00 <-",
                    Encoded = "-> $1.00 <--"
                }
            }
            select new object[]
            {
                Fallback(t.Description, fallback, t.Decoded, t.Encoded),
                t.Decoded,
                t.Encoded
            };

        static string Fallback(string description, DescriptionFallback fallback, string decoded, string encoded) =>
            description ?? ( fallback == DescriptionFallback.Decoded ? decoded
                           : fallback == DescriptionFallback.Encoded ? encoded
                           : null);
    }
}
