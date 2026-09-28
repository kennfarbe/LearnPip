# CLA rollout checklist

This is an operational checklist, not legal advice. The CLA drafts are not active until every legal and technical gate below is complete.

## Before publication

- [ ] Ask a lawyer familiar with the project rights holder's jurisdiction to review both drafts, the AGPL dual-licensing model, patent language, the continuing public AGPL commitment, electronic signature, and termination / existing grants.
- [ ] Fill in the rights recipient's legal identity and contact/notice details. Decide governing law, venue, controlling language, and signature method with counsel.
- [ ] Decide how contributions from minors will be handled. Until a reviewed guardian-consent flow exists, do not accept their code contributions.
- [ ] Confirm the project rights holder has authority to grant the commercial license. Do not represent that the CLA creates rights in third-party code or assets.
- [ ] Publish final versioned individual and entity agreements plus a contributor privacy notice describing identity/signature records, purpose, access, retention, and any service providers.
- [ ] Make the official AGPL publication promise visible before acceptance.

## Signature and merge gate

CLA Assistant is one possible service: it can request contributor assent within a pull request and report the signature status. Before using it, review the current app permissions, data handling, retention, availability, and service terms; a signature identifier may be personal data.

- [ ] Install and configure the selected CLA service for this repository only after legal and privacy review.
- [ ] Point it to the reviewed agreement version; do not point it at either draft.
- [ ] Configure the service's required status check in the repository ruleset for `main`, alongside the pull-request requirement.
- [ ] Keep the bypass list empty for normal merges. Document any narrowly scoped emergency procedure.
- [ ] Verify the check blocks a new external contributor until signature, passes after signature, remains valid on later commits as intended, and handles company-covered contributors correctly.
- [ ] Test bot and maintainer PR behavior without allowing unverified external code to merge.
- [ ] Store only the evidence needed to show who accepted which agreement version and when. Restrict access and set a retention period with counsel/privacy review.
- [ ] Update this checklist, CONTRIBUTING.md, and README.md to show the active version and signature route.

Do not mark Issue #3 complete until the final agreements are reviewed, the signer flow is active, and the required check has been tested on a sample PR.
