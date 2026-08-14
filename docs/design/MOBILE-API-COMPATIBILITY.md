# Mobile API Compatibility Layer

> **Last Updated:** 2026-06-24

## Purpose

Preserve the legacy PHP mobile bootstrap contract while introducing versioned ASP.NET endpoints for future mobile clients.

## Legacy Contract Preserved

Legacy PHP endpoint:

- `POST /App/Index`

Legacy response fields:

- `url`
- `site_url`
- `app_logo`
- `app_primary_color_code`
- `app_secondary_color_code`
- `lang_code`

ASP.NET now preserves that contract through:

- `POST /App/Index`
- `POST /api/v1/app`

Both routes return the same compatibility payload.

## Versioned Endpoints

### v1

- Route: `POST /api/v1/app`
- Purpose: backward-compatible bootstrap payload for existing mobile clients

### v2

- Route: `GET /api/v2/app/config`
- Route: `POST /api/v2/app/config`
- Purpose: expanded mobile bootstrap/config payload for newer clients

Additional v2 fields include:

- `apiVersion`
- `baseUrl`
- `siteUrl`
- `appLogoUrl`
- `primaryColor`
- `secondaryColor`
- `defaultLanguage`
- `supportedLanguages`
- `capabilities`

## Feature Toggle

The mobile API is gated by the `MobileAPI` feature toggle.

- Setting key: `MobileAPI`
- Category: `Features`
- Default seeded value: `true`

If disabled, the endpoints return `503 Service Unavailable` with a JSON error body.

## Settings Used

- `MobileApiBaseUrl`: optional override for returned API base URL
- `MobileAppLogo`: optional absolute or relative logo path for mobile clients
- `PublicSiteHomeHeroImage`: fallback mobile logo when `MobileAppLogo` is empty
- `PublicSitePrimaryColor`: primary app color
- `PublicSiteAccentColor`: secondary app color
- `DefaultLanguage`: default language code

## Notes

- The legacy PHP `zoom()` action is intentionally not carried into this compatibility layer.
- Versioned mobile routes are anonymous bootstrap/config endpoints only; authenticated mobile workflows can be added later under `/api/v2/`.