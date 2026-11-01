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
	return k.unmarshalJSON(data, true)
}

// unmarshalJSON rejects private key parameters in untrusted embedded headers.
func (k *JSONWebKey) unmarshalJSON(data []byte, allowPrivate bool) (err error) {
	var raw rawJSONWebKey
""")

patch("jwk.go",
    """	if err != nil {
		return err
	}

	certs, err := parseCertificateChain(raw.X5c)
""",
    """	if err != nil {
		return err
	}
	if !allowPrivate &&
		(raw.K != nil || raw.D != nil || raw.P != nil || raw.Q != nil ||
			raw.Dp != nil || raw.Dq != nil || raw.Qi != nil) {
		return errors.New("go-jose/go-jose: JWK contains private parameters")
	}

	certs, err := parseCertificateChain(raw.X5c)
""")

patch("jwk.go",
    """func (key rawJSONWebKey) rsaPublicKey() (*rsa.PublicKey, error) {
	if key.N == nil || key.E == nil {
		return nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, missing n/e values")
	}

	return &rsa.PublicKey{
		N: key.N.bigInt(),
		E: key.E.toInt(),
	}, nil
}
""",
    """// Matches the upstream v4 security bound; no practical RSA key exceeds this.
const learnPipMaxRSAModulusBits = 4096

func (key rawJSONWebKey) rsaPublicKey() (*rsa.PublicKey, error) {
	if key.N == nil || key.E == nil {
		return nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, missing n/e values")
	}
	n := key.N.bigInt()
	if n.BitLen() > learnPipMaxRSAModulusBits {
		return nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, modulus too large")
	}
	return &rsa.PublicKey{N: n, E: key.E.toInt()}, nil
}
""")

patch("jwk.go",
    """	rv := &rsa.PrivateKey{
		PublicKey: rsa.PublicKey{
			N: key.N.bigInt(),
			E: key.E.toInt(),
		},
		D: key.D.bigInt(),
		Primes: []*big.Int{
			key.P.bigInt(),
			key.Q.bigInt(),
		},
	}
""",
    """	n := key.N.bigInt()
	if n.BitLen() > learnPipMaxRSAModulusBits {
		return nil, fmt.Errorf("go-jose/go-jose: invalid RSA key, modulus too large")
	}
	p, q := key.P.bigInt(), key.Q.bigInt()
	if p.BitLen()+q.BitLen() > n.BitLen()+1 || new(big.Int).Mul(p, q).Cmp(n) != 0 {
		return nil, errors.New("go-jose/go-jose: invalid RSA private key, p * q != n")
	}
	rv := &rsa.PrivateKey{
		PublicKey: rsa.PublicKey{N: n, E: key.E.toInt()},
		D: key.D.bigInt(),
		Primes: []*big.Int{p, q},
	}
""")

patch("shared.go",
    """	var epk *JSONWebKey
	err := json.Unmarshal(*v, &epk)
	if err != nil {
		return nil, err
	}
	return epk, nil
""",
    """	epk := new(JSONWebKey)
	if err := epk.unmarshalJSON(*v, false); err != nil {
		return nil, err
	}
	return epk, nil
""")

patch("shared.go",
    """		case headerJWK:
			var jwk *JSONWebKey
			err = json.Unmarshal(*v, &jwk)
			if err != nil {
				err = fmt.Errorf("failed to unmarshal JWK: %v: %#v", err, string(*v))
				return
			}
			h.JSONWebKey = jwk
""",
    """		case headerJWK:
			jwk := new(JSONWebKey)
			err = jwk.unmarshalJSON(*v, false)
			if err != nil {
				err = fmt.Errorf("failed to unmarshal JWK: %w", err)
				return
			}
			h.JSONWebKey = jwk
""")

print("Upstream CVE-2026-116587 JWK bounds and private-header protections backported")
