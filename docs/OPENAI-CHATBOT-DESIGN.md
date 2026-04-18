# OpenAI Chatbot Technical Design

**Status:** Phase 7.1 Complete | **Target Phase:** Phase 7 | **Date:** April 2026

## Objective

Add an OpenAI-powered chatbot to the application using secure backend-only integration, healthcare-appropriate safety guardrails, and role-aware behavior for patients, staff, admins, and SuperAdmin users.

The chatbot should improve navigation, support, and operational help without acting as an unsafe clinical decision engine.

## Implementation Snapshot (Phase 7.1)

Implemented foundation:

- backend-only `IChatbotService` integration path (`OpenAiChatbotService`)
- role-aware prompt builder (`IChatbotPromptBuilder`)
- moderation baseline for emergency and unsafe medical prompts (`IChatbotModerationService`)
- chatbot session/message/feedback entities and DbContext mappings
- initial chatbot controller and chat page
- safe fallback responses when provider/config is unavailable

Operational companion work completed in the same delivery:

- SMTP operational health check service and CMS settings health-check action/UI

---

## Recommended Product Scope

### Primary Use Cases

- application navigation help
- FAQ/helpdesk responses
- appointment guidance
- billing/payment guidance
- patient portal help
- escalation to staff/support when the chatbot cannot resolve the issue

### Out-of-Scope by Default

- direct medical diagnosis
- medication recommendation without approved clinical workflow
- emergency triage as a final source of truth
- unrestricted access to raw patient records through prompts

The chatbot must clearly tell users to contact a real clinician or emergency services when needed.

---

## Best-Practice Architecture

### 1. Backend-Only OpenAI Access

All AI calls should go through a server-side application service such as `IChatbotService`.

Do not:

- expose OpenAI API keys in JavaScript
- call OpenAI directly from the browser
- let clients decide system prompts or authorization context

The backend service should be responsible for:

- model selection
- prompt assembly
- role-aware context building
- moderation and safety checks
- request/response logging
- retry and timeout handling

### 2. Layered Service Design

Recommended components:

- `IChatbotService`: orchestration entry point
- `IChatbotPromptBuilder`: builds safe system/user context
- `IChatbotKnowledgeService`: retrieves approved knowledge sources
- `IChatbotModerationService`: checks risky input/output
- `IChatbotAuditService`: logs sessions, failures, and feedback

This keeps AI behavior testable and avoids one oversized service.

### 3. Retrieval-Grounded Responses

For trusted answers, the bot should use approved application content rather than relying only on model memory.

Recommended knowledge sources:

- CMS pages
- notices/news relevant to operational communication
- appointment guidance rules
- billing/help guidance
- contact/support information
- selected internal FAQ/admin help content

Responses should be shaped from approved content where possible.

---

## Role-Aware Behavior Model

### Patient Role

Allowed examples:

- how to book appointments
- where to find bills or medical reports
- what a status message means
- who to contact for help

Restrictions:

- no exposure of another user’s data
- no medical diagnosis claims
- no privileged admin/staff content

### Staff/Admin Role

Allowed examples:

- workflow guidance
- module navigation help
- reminders about where to manage operational records
- explanation of system states/messages

Restrictions:

- no unrestricted access to patient details outside authorized scope
- no sensitive internal data leakage through broad prompts

### SuperAdmin Role

Allowed examples:

- configuration guidance
- system administration help
- reporting/export guidance
- chatbot configuration management if enabled

Even for SuperAdmin, chatbot answers should remain bounded by safe system rules.

---

## Safety and Healthcare Guardrails

### Required Guardrails

- emergency disclaimer for urgent symptoms/questions
- instruction to contact a healthcare professional for diagnosis/treatment
- refusal or constrained response for unsafe medical advice requests
- moderation for abusive or disallowed content
- prompt-injection resistance in system prompt and retrieval pipeline

### Response Strategy

When confidence is low, the bot should:

- state that it is not confident
- avoid inventing answers
- offer navigation/help alternatives
- escalate to support or staff contact information

### Suggested System Behavior Rules

- answer only within approved healthcare support scope
- do not fabricate policies, schedules, prices, or diagnosis
- do not claim to have performed clinical review unless an actual workflow provides it
- prefer concise operational guidance over speculative detail

---

## Privacy and Security Design

### Data Minimization

Only send the minimum required context to OpenAI.

Recommended rules:

- avoid sending raw PHI/PII unless absolutely necessary for the approved use case
- mask or omit identifiers wherever possible
- never send full patient records by default
- build role-scoped context on the server only

### Secrets and Configuration

- store API keys in secure configuration/secrets
- support environment-based model selection
- support request timeout, token caps, and retry settings
- log usage metrics without leaking raw secrets or sensitive prompt content

### Abuse Prevention

- rate limiting per user/session/IP
- throttling for anonymous or public chatbot entry points
- moderation checks for input and optionally output
- transcript retention policy with redaction where appropriate

---

## Feature Set Proposal

### Phase 1 Chatbot Features

- chat widget or dedicated chat page
- conversation history in-session
- retry on transient failures
- typing indicator and error state
- feedback buttons (`helpful` / `not helpful`)
- escalation link/contact handoff

### Phase 2 Enhancements

- multilingual support
- retrieval-backed knowledge search
- admin-managed canned prompt templates
- analytics dashboard for unresolved questions and handoffs

### Phase 3 Controlled Advanced Features

- authenticated patient-specific guidance only if strong authorization and data-scoping controls are in place
- workflow-triggered assistant suggestions for staff/admin users

---

## Data Model Proposal

### ChatSession

Suggested fields:

- `Id`
- `UserId`
- `Role`
- `StartedAtUtc`
- `EndedAtUtc`
- `Status`
- `Channel`

### ChatMessage

Suggested fields:

- `Id`
- `SessionId`
- `SenderType`
- `Content`
- `CreatedAtUtc`
- `ModerationStatus`
- `TokenCount`

### ChatFeedback

Suggested fields:

- `Id`
- `SessionId`
- `MessageId`
- `FeedbackType`
- `Comment`
- `CreatedAtUtc`

### ChatbotSettings

Suggested fields:

- `Enabled`
- `EnabledForPatients`
- `EnabledForStaff`
- `EnabledForAdmins`
- `ModelName`
- `Temperature`
- `MaxTokens`
- `SystemPromptVersion`
- `ModerationEnabled`

---

## Prompt and Retrieval Strategy

### Prompt Assembly Principles

- construct system prompt server-side
- include user role and allowed scope
- inject only approved retrieved content
- do not include hidden internal instructions from user-controlled input

### Retrieval Strategy

Recommended initial approach:

- keyword/vector retrieval over curated FAQ/CMS/help content
- role-filtered retrieval set
- source metadata retained in backend logs

### Suggested Answer Pattern

1. detect intent
2. retrieve approved content
3. build safe scoped prompt
4. call OpenAI
5. run post-response validation/moderation
6. return answer or fallback/escalation message

---

## Admin Controls

Recommended admin controls:

- enable/disable chatbot globally
- enable/disable by role
- configure model and request limits
- configure base system prompt
- manage supported FAQ/knowledge content
- review analytics and failure trends

Recommended SuperAdmin controls:

- feature flag management
- moderation setting overrides
- transcript retention settings
- emergency disable switch

---

## Testing Strategy

### Unit Tests

- prompt builder role scoping
- retrieval filtering by role/content type
- fallback behavior on empty/low-confidence answer
- moderation decision handling

### Integration Tests

- backend service calls OpenAI through a single controlled path
- unauthorized data is not included in prompt context
- feature flag and role-based visibility work correctly
- transcript logging and feedback recording work as expected

### Adversarial Testing

- prompt injection attempts
- attempts to retrieve other users’ data
- requests for unsafe medical advice
- token exhaustion and rate-limit scenarios

---

## Rollout Plan

### Recommended Rollout Sequence

1. internal admin/staff pilot only
2. limited patient pilot for navigation/help use cases
3. broader rollout after moderation, analytics, and fallback quality are stable

### Release Controls

- ship behind feature flags
- monitor usage, error rate, handoff rate, and negative feedback
- keep fast rollback path available through settings

---

## Open Decisions

Before implementation, these should be finalized:

1. Which roles should see the chatbot on day one?
2. Should the initial version be staff-only, patient-only, or both?
3. Which exact sources are approved for grounding the bot?
4. Will transcripts be stored in full, summarized, redacted, or partially disabled?
5. Should the bot answer only support questions in v1, or also perform guided workflow assistance?

---

## Recommended Implementation Order

1. Build backend-only AI service and settings model
2. Add safe chat UI and feature flag controls
3. Add curated knowledge retrieval
4. Add moderation, transcript logging, and analytics
5. Pilot with restricted users before wider rollout

---

**End of OpenAI Chatbot Design**