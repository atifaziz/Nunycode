# Nunycode

[![Build Status][win-build-badge]][win-builds]
[![Build Status][nix-build-badge]][nix-builds]
[![NuGet][nuget-badge]][nuget-pkg]
[![MyGet][myget-badge]][edge-pkgs]

Nunycode is a .NET Standard library that implements Punycode conversion
in compliance to [RFC 3492][rfc3492] and [RFC 5891][rfc5891].

It was ported from [punycode.js][punycode.js], which was bundled with Node.js
from v0.6.2+ until its soft-deprecation in v7.

[rfc3492]: https://tools.ietf.org/html/rfc3492
[rfc5891]: https://tools.ietf.org/html/rfc5891
[punycode.js]: https://github.com/bestiejs/punycode.js


## API


### `Punycode.Decode`

Converts a Punycode string of ASCII symbols to a string of Unicode symbols.

```c#
// decode domain name parts
Console.WriteLine(Punycode.Decode("maana-pta")); // "mañana"
Console.WriteLine(Punycode.Decode("--dqo34k"));  // "☃-⌘"
```


### `Punycode.Encode`

Converts a string of Unicode symbols to a Punycode string of ASCII symbols.

```c#
// encode domain name parts
Console.WriteLine(Punycode.Encode("mañana")); // "maana-pta"
Console.WriteLine(Punycode.Encode("☃-⌘"));    // "--dqo34k"
```


### `Punycode.ToUnicode`

Converts a Punycode string representing a domain name or an e-mail address to
Unicode. Only the Punycoded parts of the input will be converted, i.e. it
doesn't matter if you call it on a string that has already been converted to
Unicode.

```c#
// decode domain names
Console.WriteLine(Punycode.ToUnicode("xn--maana-pta.com"));
// → "mañana.com"
Console.WriteLine(Punycode.ToUnicode("xn----dqo34k.com"));
// → "☃-⌘.com"

// decode email addresses
Console.WriteLine(Punycode.ToUnicode("джумла@xn--p-8sbkgc5ag7bhce.xn--ba-lmcq"));
// → "джумла@джpумлатест.bрфa"
```


### `Punycode.ToASCII`

Converts a lowercased Unicode string representing a domain name or an e-mail
address to Punycode. Only the non-ASCII parts of the input will be converted,
i.e. it doesn't matter if you call it with a domain that's already in ASCII.

```c#
// encode domain names
Console.WriteLine(Punycode.ToAscii("mañana.com"));
// → "xn--maana-pta.com"
Console.WriteLine(Punycode.ToAscii("☃-⌘.com"));
// → "xn----dqo34k.com"

// encode email addresses
Console.WriteLine(Punycode.ToAscii("джумла@джpумлатест.bрфa"));
// → "джумла@xn--p-8sbkgc5ag7bhce.xn--ba-lmcq"
```


### `Ucs2.Decode`

Creates an array containing the numeric code point values of each Unicode
symbol in the string. While .NET uses UCS-2 internally, this function will
convert a pair of surrogate halves (each of which UCS-2 exposes as separate
characters) into a single code point, matching UTF-16.

```c#
Console.WriteLine(Ucs2.Decode("abc"));
// → [97, 98, 99]
// → [0x61, 0x62, 0x63]
// surrogate pair for U+1D306 TETRAGRAM FOR CENTRE:
Console.WriteLine(Ucs2.Decode("\uD834\uDF06"));;
// → [119558]
// → [0x1D306]
```


### `Ucs2.Encode`

Creates a string based on an array of numeric code point values.

```c#
Console.WriteLine(Ucs2.Encode(0x61, 0x62, 0x63));
// → "abc"
Console.WriteLine(Ucs2.Encode(0x1D306));
// → "\uD834\uDF06"
```


[win-build-badge]: https://img.shields.io/appveyor/ci/raboof/nunycode/master.svg?label=windows
[win-builds]: https://ci.appveyor.com/project/raboof/nunycode
[nix-build-badge]: https://img.shields.io/travis/atifaziz/Nunycode/master.svg?label=linux
[nix-builds]: https://travis-ci.org/atifaziz/Nunycode
[myget-badge]: https://img.shields.io/myget/raboof/vpre/Nunycode.svg?label=myget
[edge-pkgs]: https://www.myget.org/feed/raboof/package/nuget/Nunycode
[nuget-badge]: https://img.shields.io/nuget/v/Nunycode.svg
[nuget-pkg]: https://www.nuget.org/packages/Nunycode
