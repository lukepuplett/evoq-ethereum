# Formal Specification of the Encoding

https://docs.soliditylang.org/en/latest/abi-spec.html

We distinguish static and dynamic types. Static types are encoded in-place and dynamic types are encoded at a separately allocated location after the current block.

**Definition**: The following types are called “dynamic”:

- `bytes`
- `string`
- `T[]` for any `T`
- `T[k]` for any dynamic `T` and any `k >= 0`

All other types are called “static”.

**Definition**: `len(a)` is the number of bytes in a binary string `a`. The type of `len(a)` is assumed to be `uint256`.

We define `enc`, the actual encoding, as a mapping of values of the ABI types to binary strings such that `len(enc(X))` depends on the value of `X` if and only if the type of `X` is dynamic.

**Definition**: For any ABI value `X`, we recursively define `enc(X)`, depending on the type of `X` being

- `(T1,...,Tk)` for `k >= 0` and any types `T1, ..., Tk`

    `enc(X) = head(X(1)) ... head(X(k)) tail(X(1)) ... tail(X(k))`

    where `X = (X(1), ..., X(k))` and `head` and `tail` are defined for `Ti` as follows:

    if `Ti` is static:

    `head(X(i)) = enc(X(i))` and `tail(X(i)) = ""` (the empty string)

    otherwise, i.e. if `Ti` is dynamic:

    `head(X(i)) = enc(len( head(X(1)) ... head(X(k)) tail(X(1)) ... tail(X(i-1)) ))` and `tail(X(i)) = enc(X(i))`

    Note that in the dynamic case, `head(X(i))` is well-defined since the lengths of the head parts only depend on the types and not the values. The value of `head(X(i))` is the offset of the beginning of `tail(X(i))` relative to the start of `enc(X)`.

- `T[k]` for any `T` and `k`:

    `enc(X) = enc((X[0], ..., X[k-1]))`

    i.e. it is encoded as if it were a tuple with `k` elements of the same type.

- `T[]` where `X` has `k` elements (which is assumed to be of type `uint256`):

    `enc(X) = enc(k) enc((X[0], ..., X[k-1]))`

    i.e. it is encoded as if it were a tuple with `k` elements of the same type (resp. an array of static size `k`), prefixed with the number of elements.

- `bytes`, of length `k` (which is assumed to be of type `uint256`):

    `enc(X) = enc(k) pad_right(X)`

    i.e. the number of bytes is encoded as a `uint256` followed by the actual value of `X` as a byte sequence, followed by the minimum number of zero-bytes such that `len(enc(X))` is a multiple of 32.

- `string`:

    `enc(X) = enc(enc_utf8(X))`, i.e. `X` is UTF-8 encoded and this value is interpreted as of bytes type and encoded further. Note that the length used in this subsequent encoding is the number of bytes of the UTF-8 encoded string, not its number of characters.

- `uint<M>`: `enc(X)` is the big-endian encoding of `X`, padded on the higher-order (left) side with zero-bytes such that the length is 32 bytes.

- `address`: as in the uint160 case

- `int<M>`: `enc(X)` is the big-endian two’s complement encoding of `X`, padded on the higher-order (left) side with 0xff bytes for negative `X` and with zero-bytes for non-negative `X` such that the length is 32 bytes.

- `bool`: as in the `uint8` case, where `1` is used for `true` and `0` for `false`

- `fixed<M>x<N>`: `enc(X)` is `enc(X * 10**N)` where `X * 10**N` is interpreted as a `int256`.

- `fixed`: as in the `fixed128x18` case

- `ufixed<M>x<N>`: `enc(X)` is `enc(X * 10**N)` where `X * 10**N` is interpreted as a `uint256`.

- `ufixed`: as in the `ufixed128x18` case

- `bytes<M>`: `enc(X)` is the sequence of bytes in `X` padded with trailing zero-bytes to a length of 32 bytes.

Note that for any `X`, `len(enc(X))` is a multiple of 32.

## Function Selector and Argument Encoding

All in all, a call to the function `f` with parameters `a_1, ..., a_n` is encoded as

`function_selector(f) enc((a_1, ..., a_n))`

and the return values `v_1, ..., v_k` of `f` are encoded as

`enc((v_1, ..., v_k))`

i.e. the values are combined into a tuple and encoded.

## Examples

Given the contract:

```solidity
// SPDX-License-Identifier: GPL-3.0
pragma solidity >=0.4.16 <0.9.0;

contract Foo {
    function bar(bytes3[2] memory) public pure {}
    function baz(uint32 x, bool y) public pure returns (bool r) { r = x > 32 || y; }
    function sam(bytes memory, bool, uint[] memory) public pure {}
}
```

---

### `bar(bytes3[2] memory)`

Thus, for our Foo example, if we wanted to call bar with the argument `["abc", "def"]`, we would pass 68 bytes total, broken down into:

`0xfce353f6`: the Method ID. This is derived from the signature `bar(bytes3[2])`.

`0x6162630000000000000000000000000000000000000000000000000000000000`: the first part of the first parameter, a `bytes3` value "abc" (left-aligned).

`0x6465660000000000000000000000000000000000000000000000000000000000`: the second part of the first parameter, a `bytes3` value "def" (left-aligned).

In total:

```
0xfce353f661626300000000000000000000000000000000000000000000000000000000006465660000000000000000000000000000000000000000000000000000000000
```

---

### `baz(uint32,bool)`

If we wanted to call baz with the parameters 69 and true, we would pass 68 bytes total, which can be broken down into:

`0xcdcd77c0`: the Method ID. This is derived as the first 4 bytes of the Keccak hash of the ASCII form of the signature `baz(uint32,bool)`.

`0x0000000000000000000000000000000000000000000000000000000000000045`: the first parameter, a `uint32` value 69 padded to 32 bytes

`0x0000000000000000000000000000000000000000000000000000000000000001`: the second parameter - boolean true, padded to 32 bytes

In total:

```
0xcdcd77c000000000000000000000000000000000000000000000000000000000000000450000000000000000000000000000000000000000000000000000000000000001
```

It returns a single bool. If, for example, it were to return false, its output would be the single byte array `0x0000000000000000000000000000000000000000000000000000000000000000`, a single bool.

---

### `sam(bytes,bool,uint[] memory)`

If we wanted to call sam with the arguments `"dave"`, `true` and `[1,2,3]`, we would pass 292 bytes total, broken down into:

`0xa5643bf2`: the Method ID. This is derived from the signature `sam(bytes,bool,uint256[])`. Note that `uint` is replaced with its canonical representation `uint256`.

`0x0000000000000000000000000000000000000000000000000000000000000060`: the location of the data part of the first parameter (dynamic type), measured in bytes from the start of the arguments block. In this case, `0x60`.

`0x0000000000000000000000000000000000000000000000000000000000000001`: the second parameter: boolean true.

`0x00000000000000000000000000000000000000000000000000000000000000a0`: the location of the data part of the third parameter (dynamic type), measured in bytes. In this case, `0xa0`.

`0x0000000000000000000000000000000000000000000000000000000000000004`: the data part of the first argument, it starts with the length of the byte array in elements, in this case, 4.

`0x6461766500000000000000000000000000000000000000000000000000000000`: the contents of the first argument: the UTF-8 (equal to ASCII in this case) encoding of "dave", padded on the right to 32 bytes.

`0x0000000000000000000000000000000000000000000000000000000000000003`: the data part of the third argument, it starts with the length of the array in elements, in this case, 3.

`0x0000000000000000000000000000000000000000000000000000000000000001`: the first entry of the third parameter.

`0x0000000000000000000000000000000000000000000000000000000000000002`: the second entry of the third parameter.

`0x0000000000000000000000000000000000000000000000000000000000000003`: the third entry of the third parameter.

In total:

```
0xa5643bf20000000000000000000000000000000000000000000000000000000000000060000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000a0000000000000000000000000000000000000000000000000000000000000000464617665000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000020000000000000000000000000000000000000000000000000000000000000003
```


## Use of Dynamic Types

### `f(uint256,uint32[],bytes10,bytes)`

A call to a function with the signature `f(uint256,uint32[],bytes10,bytes)` with values `(0x123, [0x456, 0x789], "1234567890", "Hello, world!")` is encoded in the following way:

We take the first four bytes of `keccak("f(uint256,uint32[],bytes10,bytes)")`, i.e. `0x8be65246`. Then we encode the head parts of all four arguments. For the static types `uint256` and `bytes10`, these are directly the values we want to pass, whereas for the dynamic types `uint32[]` and `bytes`, we use the offset in bytes to the start of their data area, measured from the start of the value encoding (i.e. not counting the first four bytes containing the hash of the function signature). These are:

`0x0000000000000000000000000000000000000000000000000000000000000123` (0x123 padded to 32 bytes)

`0x0000000000000000000000000000000000000000000000000000000000000080` (offset to start of data part of second parameter, 4*32 bytes, exactly the size of the head part)

`0x3132333435363738393000000000000000000000000000000000000000000000` ("1234567890" padded to 32 bytes on the right)

`0x00000000000000000000000000000000000000000000000000000000000000e0` (offset to start of data part of fourth parameter = offset to start of data part of first dynamic parameter + size of data part of first dynamic parameter = 4*32 + 3*32 (see below))

After this, the data part of the first dynamic argument, `[0x456, 0x789]` follows:

`0x0000000000000000000000000000000000000000000000000000000000000002` (number of elements of the array, 2)

`0x0000000000000000000000000000000000000000000000000000000000000456` (first element)

`0x0000000000000000000000000000000000000000000000000000000000000789` (second element)

Finally, we encode the data part of the second dynamic argument, `"Hello, world!"`:

`0x000000000000000000000000000000000000000000000000000000000000000d` (number of elements (bytes in this case): 13)

`0x48656c6c6f2c20776f726c642100000000000000000000000000000000000000` ("Hello, world!" padded to 32 bytes on the right)

All together, the encoding is (newline after function selector and each 32-bytes for clarity):

```
0x8be65246
  0000000000000000000000000000000000000000000000000000000000000123
  0000000000000000000000000000000000000000000000000000000000000080
  3132333435363738393000000000000000000000000000000000000000000000
  00000000000000000000000000000000000000000000000000000000000000e0
  0000000000000000000000000000000000000000000000000000000000000002
  0000000000000000000000000000000000000000000000000000000000000456
  0000000000000000000000000000000000000000000000000000000000000789
  000000000000000000000000000000000000000000000000000000000000000d
  48656c6c6f2c20776f726c642100000000000000000000000000000000000000
```

---

### `g(uint256[][],string[])`

Let us apply the same principle to encode the data for a function with a signature `g(uint256[][],string[])` with values `([[1, 2], [3]], ["one", "two", "three"])` but start from the most atomic parts of the encoding:

First we encode the length and data of the first embedded dynamic array `[1, 2]` of the first root array `[[1, 2], [3]]`:

`0x0000000000000000000000000000000000000000000000000000000000000002` (number of elements in the first array, 2; the elements themselves are 1 and 2)

`0x0000000000000000000000000000000000000000000000000000000000000001` (first element)

`0x0000000000000000000000000000000000000000000000000000000000000002` (second element)

Then we encode the length and data of the second embedded dynamic array `[3]` of the first root array `[[1, 2], [3]]`:

`0x0000000000000000000000000000000000000000000000000000000000000001` (number of elements in the second array, 1; the element is 3)

`0x0000000000000000000000000000000000000000000000000000000000000003` (first element)

Then we need to find the offsets `a` and `b` for their respective dynamic arrays `[1, 2]` and `[3]`. To calculate the offsets we can take a look at the encoded data of the first root array `[[1, 2], [3]]` enumerating each line in the encoding:

```
0 - a                                                                - offset of [1, 2]
1 - b                                                                - offset of [3]
2 - 0000000000000000000000000000000000000000000000000000000000000002 - count for [1, 2]
3 - 0000000000000000000000000000000000000000000000000000000000000001 - encoding of 1
4 - 0000000000000000000000000000000000000000000000000000000000000002 - encoding of 2
5 - 0000000000000000000000000000000000000000000000000000000000000001 - count for [3]
6 - 0000000000000000000000000000000000000000000000000000000000000003 - encoding of 3
```

Offset `a` points to the start of the content of the array `[1, 2]` which is line 2 (64 bytes); thus `a = 0x0000000000000000000000000000000000000000000000000000000000000040`.

Offset `b` points to the start of the content of the array `[3]` which is line 5 (160 bytes); thus `b = 0x00000000000000000000000000000000000000000000000000000000000000a0`.

Then we encode the embedded strings of the second root array:

`0x0000000000000000000000000000000000000000000000000000000000000003` (number of characters in word "one")

`0x6f6e650000000000000000000000000000000000000000000000000000000000` (utf8 representation of word "one")

`0x0000000000000000000000000000000000000000000000000000000000000003` (number of characters in word "two")

`0x74776f0000000000000000000000000000000000000000000000000000000000` (utf8 representation of word "two")

`0x0000000000000000000000000000000000000000000000000000000000000005` (number of characters in word "three")

`0x7468726565000000000000000000000000000000000000000000000000000000` (utf8 representation of word "three")

In parallel to the first root array, since strings are dynamic elements we need to find their offsets `c`, `d` and `e`:

```
0 - c                                                                - offset for "one"
1 - d                                                                - offset for "two"
2 - e                                                                - offset for "three"
3 - 0000000000000000000000000000000000000000000000000000000000000003 - count for "one"
4 - 6f6e650000000000000000000000000000000000000000000000000000000000 - encoding of "one"
5 - 0000000000000000000000000000000000000000000000000000000000000003 - count for "two"
6 - 74776f0000000000000000000000000000000000000000000000000000000000 - encoding of "two"
7 - 0000000000000000000000000000000000000000000000000000000000000005 - count for "three"
8 - 7468726565000000000000000000000000000000000000000000000000000000 - encoding of "three"
```

Offset `c` points to the start of the content of the string "one" which is line 3 (96 bytes); thus `c = 0x0000000000000000000000000000000000000000000000000000000000000060`.

Offset `d` points to the start of the content of the string "two" which is line 5 (160 bytes); thus `d = 0x00000000000000000000000000000000000000000000000000000000000000a0`.

Offset `e` points to the start of the content of the string "three" which is line 7 (224 bytes); thus `e = 0x00000000000000000000000000000000000000000000000000000000000000e0`.

Note that the encodings of the embedded elements of the root arrays are not dependent on each other and have the same encodings for a function with a signature `g(string[],uint256[][])`.

Then we encode the length of the first root array:

`0x0000000000000000000000000000000000000000000000000000000000000002` (number of elements in the first root array, 2; the elements themselves are `[1, 2]` and `[3]`)

Then we encode the length of the second root array:

`0x0000000000000000000000000000000000000000000000000000000000000003` (number of strings in the second root array, 3; the strings themselves are "one", "two" and "three")

Finally we find the offsets `f` and `g` for their respective root dynamic arrays `[[1, 2], [3]]` and `["one", "two", "three"]`, and assemble parts in the correct order:

```
0x2289b18c                                                            - function signature
 0 - f                                                                - offset of [[1, 2], [3]]
 1 - g                                                                - offset of ["one", "two", "three"]
 2 - 0000000000000000000000000000000000000000000000000000000000000002 - count for [[1, 2], [3]]
 3 - 0000000000000000000000000000000000000000000000000000000000000040 - offset of [1, 2]
 4 - 00000000000000000000000000000000000000000000000000000000000000a0 - offset of [3]
 5 - 0000000000000000000000000000000000000000000000000000000000000002 - count for [1, 2]
 6 - 0000000000000000000000000000000000000000000000000000000000000001 - encoding of 1
 7 - 0000000000000000000000000000000000000000000000000000000000000002 - encoding of 2
 8 - 0000000000000000000000000000000000000000000000000000000000000001 - count for [3]
 9 - 0000000000000000000000000000000000000000000000000000000000000003 - encoding of 3
10 - 0000000000000000000000000000000000000000000000000000000000000003 - count for ["one", "two", "three"]
11 - 0000000000000000000000000000000000000000000000000000000000000060 - offset for "one"
12 - 00000000000000000000000000000000000000000000000000000000000000a0 - offset for "two"
13 - 00000000000000000000000000000000000000000000000000000000000000e0 - offset for "three"
14 - 0000000000000000000000000000000000000000000000000000000000000003 - count for "one"
15 - 6f6e650000000000000000000000000000000000000000000000000000000000 - encoding of "one"
16 - 0000000000000000000000000000000000000000000000000000000000000003 - count for "two"
17 - 74776f0000000000000000000000000000000000000000000000000000000000 - encoding of "two"
18 - 0000000000000000000000000000000000000000000000000000000000000005 - count for "three"
19 - 7468726565000000000000000000000000000000000000000000000000000000 - encoding of "three"
```

Offset `f` points to the start of the content of the array `[[1, 2], [3]]` which is line 2 (64 bytes); thus `f = 0x0000000000000000000000000000000000000000000000000000000000000040`.

Offset `g` points to the start of the content of the array `["one", "two", "three"]` which is line 10 (320 bytes); thus `g = 0x0000000000000000000000000000000000000000000000000000000000000140`.

## Notes

 - The specification is not clear at all and lacks more complex examples.

 - Consider a design where each EvmParam is able to encode itself into a SlotCollection, evmParm.Encode(v, slots) and this can cascade down into nested parameters.

### Other Learning Resources

 - https://www.rareskills.io/post/abi-encoding