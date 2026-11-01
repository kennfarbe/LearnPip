package jose

import (
  "encoding/json"
  "math/big"
  "strings"
  "testing"
)

// Regression checks for CVE-2026-116587. The unpatched upstream v3 would
// accept oversized keys or do expensive RSA validation for mismatched primes.
func TestLearnPipRejectOversizedRSAPublicKey(t *testing.T) {
  bigN := new(big.Int).Lsh(big.NewInt(1), 5000)
  key := rawJSONWebKey{N: newBuffer(bigN.Bytes()), E: newBufferFromInt(65537)}
  _, err := key.rsaPublicKey()
  if err == nil || !strings.Contains(err.Error(), "modulus too large") {
    t.Fatalf("oversized public modulus not rejected: %v", err)
  }
}

func TestLearnPipRejectOversizedRSAPrivateKey(t *testing.T) {
  bigN := new(big.Int).Lsh(big.NewInt(1), 5000)
  key := rawJSONWebKey{
    N: newBuffer(bigN.Bytes()), E: newBufferFromInt(65537),
    D: newBufferFromInt(1), P: newBufferFromInt(1), Q: newBufferFromInt(1),
  }
  _, err := key.rsaPrivateKey()
  if err == nil || !strings.Contains(err.Error(), "modulus too large") {
    t.Fatalf("oversized private modulus not rejected: %v", err)
  }
}

func TestLearnPipRejectMismatchedPrimesBeforeRSAValidation(t *testing.T) {
  p := new(big.Int).Lsh(big.NewInt(1), 100000)
  key := rawJSONWebKey{
    N: newBufferFromInt(3233), E: newBufferFromInt(17),
    D: newBufferFromInt(2753), P: newBuffer(p.Bytes()), Q: newBufferFromInt(53),
  }
  _, err := key.rsaPrivateKey()
  if err == nil || !strings.Contains(err.Error(), "p * q != n") {
    t.Fatalf("mismatched primes not rejected: %v", err)
  }
}

func TestLearnPipRejectPrivateJWKInUntrustedHeader(t *testing.T) {
  privateKey, _ := json.Marshal(rawJSONWebKey{Kty: "RSA", D: newBufferFromInt(3)})
  key := new(JSONWebKey)
  err := key.unmarshalJSON(privateKey, false)
  if err == nil || !strings.Contains(err.Error(), "private parameters") {
    t.Fatalf("untrusted header did not reject private key: %v", err)
  }
}
