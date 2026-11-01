#!/usr/bin/env python3
"""Backport the upstream RSA/JWK DoS protections to pinned go-jose/v3.0.5.

CVE-2026-116587, upstream v4 change a0111f82742fa92bceec9ede40845e73319e6230.
This is a source-level security patch, not an advisory suppression.
Fail closed when upstream source no longer matches the reviewed base.
"""
from pathlib import Path
import sys

root = Path(sys.argv[1])
if not (root / "go.mod").is_file() or "module github.com/go-jose/go-jose/v3" not in (root / "go.mod").read_text():
    raise SystemExit("Expected pinned go-jose/v3 source")


def patch(path, old, new):
    file = root / path
    data = file.read_text()
    if data.count(old) != 1:
        raise SystemExit(f"{path}: expected exact upstream source anchor once; found {data.count(old)}")
    file.write_text(data.replace(old, new, 1))


patch("jwk.go",
    "func (k *JSONWebKey) UnmarshalJSON(data []byte) (err error) {\n\tvar raw rawJSONWebKey\n",
    """func (k *JSONWebKey) UnmarshalJSON(data []byte) (err error) {
\treturn k.unmarshalJSON(data, true)
}

// unmarshalJSON rejects private key parameters in untrusted embedded headers.
func (k *JSONWebKey) unmarshalJSON(data []byte, allowPrivate bool) (err error) {
\tvar raw rawJSONWebKey
""")

patch("jwk.go",
    """\tif err != nil {
\t\treturn err
\t}

\tcerts, err := parseCertificateChain(raw.X5c)
""",
    """\tif err != nil {
\t\treturn err
\t}
\tif !allowPrivate &&
\t\t(raw.K != nil || raw.D != nil || raw.P != nil || raw.Q != nil ||
\t\t\traw.Dp != nil || raw.Dq != nil || raw.Qi != nil) {
\t\treturn errors.New("go-jose/go-jose: JWK contains private parameters")
\t}

\tcerts, err := parseCertificateChain(raw.X5c)
""")

patch("jwk.go",
    """func (key rawJSONWebKey) rsaPublicKey() (*rsa.PublicKey, error) {
\tif key.N == nil || key.E == nil {
\t\treturn nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, missing n/e values")
\t}

\treturn &rsa.PublicKey{
\t\tN: key.N.bigInt(),
\t\tE: key.E.toInt(),
\t}, nil
}
""",
    """// Matches the upstream v4 security bound; no practical RSA key exceeds this.
const learnPipMaxRSAModulusBits = 4096

func (key rawJSONWebKey) rsaPublicKey() (*rsa.PublicKey, error) {
\tif key.N == nil || key.E == nil {
\t\treturn nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, missing n/e values")
\t}
\tn := key.N.bigInt()
\tif n.BitLen() > learnPipMaxRSAModulusBits {
\t\treturn nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, modulus too large")
\t}
\treturn &rsa.PublicKey{N: n, E: key.E.toInt()}, nil
}
""")

patch("jwk.go",
    """\trv := &rsa.PrivateKey{
\t\tPublicKey: rsa.PublicKey{
\t\t\tN: key.N.bigInt(),
\t\t\tE: key.E.toInt(),
\t\t},
\t\tD: key.D.bigInt(),
\t\tPrimes: []*big.Int{
\t\t\tkey.P.bigInt(),
\t\t\tkey.Q.bigInt(),
\t\t},
\t}
""",
    """\tn := key.N.bigInt()
\tif n.BitLen() > learnPipMaxRSAModulusBits {
\t\treturn nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, modulus too large")
\t}
\tp, q := key.P.bigInt(), key.Q.bigInt()
\tif p.BitLen()+q.BitLen() > n.BitLen()+1 || new(big.Int).Mul(p, q).Cmp(n) != 0 {
\t\treturn nil, errors.New("go-jose/go-jose: invalid RSA private key, p * q != n")
\t}
\trv := &rsa.PrivateKey{
\t\tPublicKey: rsa.PublicKey{N: n, E: key.E.toInt()},
\t\tD: key.D.bigInt(),
\t\tPrimes: []*big.Int{p, q},
\t}
""")

patch("shared.go",
    """\tvar epk *JSONWebKey
\terr := json.Unmarshal(*v, &epk)
\tif err != nil {
\t\treturn nil, err
\t}
\treturn epk, nil
""",
    """\tepk := new(JSONWebKey)
\tif err := epk.unmarshalJSON(*v, false); err != nil {
\t\treturn nil, err
\t}
\treturn epk, nil
""")

patch("shared.go",
    """\t\tcase headerJWK:
\t\t\tvar jwk *JSONWebKey
\t\t\terr = json.Unmarshal(*v, &jwk)
\t\t\tif err != nil {
\t\t\t\terr = fmt.Errorf("failed to unmarshal JWK: %v: %#v", err, string(*v))
\t\t\t\treturn
\t\t\t}
\t\t\th.JSONWebKey = jwk
""",
    """\t\tcase headerJWK:
\t\t\tjwk := new(JSONWebKey)
\t\t\terr = jwk.unmarshalJSON(*v, false)
\t\t\tif err != nil {
\t\t\t\terr = fmt.Errorf("failed to unmarshal JWK: %w", err)
\t\t\t\treturn
\t\t\t}
\t\t\th.JSONWebKey = jwk
""")

print("Upstream CVE-2026-116587 JWK bounds and private-header protections backported")
