# MedyxHMS Lic Operator Guide


> Last Updated: 2026-04-22
> Operational Baseline: runtime stabilization complete, automated UAT technical gates passing.
> References: docs/UAT-EXECUTION-EVIDENCE-2026-04-22.md, docs/UPDATED-PRD-2026-04-22.md, docs/UPDATED-TODO-LIST-2026-04-22.md

## Audience
- SuperAdmin (primary)
- Admin (read-only operational awareness)

## Purpose
This guide explains how to:
1. run MedyxHMS-Lic
2. generate a signed MedyxHMS.lic file
3. upload the license file into the ASP.NET MedyxHMS system

## Prerequisites
- SuperAdmin access to MedyxHMS ASP.NET License module.
- Access to MedyxHMS-Lic executable.
- Valid operator details and module entitlement decisions.

## Important Rules Before You Start
- License upload is SuperAdmin-only.
- Verification key is one-time use after successful upload.
- Each new upload cycle should use a newly generated key pair from MedyxHMS-Lic.
- Editing license content manually invalidates signature/verification checks.

## Part A - Run MedyxHMS-Lic and Generate Keys
1. Launch MedyxHMS-Lic on a secure admin workstation.
2. Generate a new RSA key pair.
3. Save the output key files in a secure folder.
4. Copy the public key values shown by tool:
   - modulus (hex)
   - exponent (hex)
   - verification key/fingerprint

## Part B - Prepare ASP.NET for License Upload
1. Sign in to ASP.NET MedyxHMS as SuperAdmin.
2. Open License screen.
3. Paste the public key values from MedyxHMS-Lic into the upload key fields:
   - public key modulus
   - public key exponent
   - verification key
4. Save/apply key configuration.

## Part C - Generate MedyxHMS.lic File
1. In MedyxHMS-Lic, choose license duration:
   - 1 month, 1 year, 2 years, 3 years, or custom date (minimum one month from issue date).
2. Select licensed modules using checklist.
3. Generate license output.
4. Ensure filename is MedyxHMS.lic.

## Part D - Upload License into ASP.NET System
1. Return to License screen in ASP.NET.
2. Use Upload/Import action and select MedyxHMS.lic.
3. Submit upload.
4. Confirm success message.

## Part E - Post-Upload Verification Checklist
1. Confirm current expiry is updated.
2. Confirm status is Active (or expected state).
3. Confirm entitlement matrix shows correct Licensed/Locked modules.
4. Optionally export entitlement matrix CSV for evidence.
5. Confirm upload key fields are cleared after successful upload (one-time key consumption behavior).

## Troubleshooting
- Verification key mismatch:
  - Ensure uploaded MedyxHMS.lic was generated using the same public key values currently configured in ASP.NET.
- Signature invalid:
  - Regenerate file in MedyxHMS-Lic; do not edit generated content manually.
- Upload rejected due to reused key:
  - Generate new key pair in MedyxHMS-Lic and repeat full process.
- Locked module despite upload:
  - Confirm module was selected in MedyxHMS-Lic entitlement checklist before generation.

## Security and Handling Best Practices
- Do not share private key files.
- Use secure operator machine and restricted folder permissions.
- Keep a controlled archive of:
  - generated MedyxHMS.lic
  - upload timestamp
  - operator identity
  - exported entitlement snapshot
- Use audit logs in ASP.NET to validate who performed license update actions.

## Related Docs
- [../license Guide.md](../license%20Guide.md)
- [../LICENSING-DESIGN.md](../LICENSING-DESIGN.md)
- [SuperAdmin.md](SuperAdmin.md)
