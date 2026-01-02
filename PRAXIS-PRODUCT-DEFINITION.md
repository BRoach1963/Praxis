# Praxis Product Definition

**Product Name:** Praxis  
**Developer:** Prickly Cactus Software  
**Platform:** Desktop-first (WPF, .NET)  
**Target Users:** Therapists, counselors, and small mental health practices

---

## What Praxis Is

Praxis is a **calm, private workspace for reflective professional practice**.

It helps clinicians:

- Maintain continuity across sessions
- Track client goals, themes, and progress over time
- Prepare for sessions thoughtfully
- Reflect after sessions without administrative pressure
- Separate "official" progress notes from private clinical thinking
- Reduce cognitive load caused by fragmented systems

### Core Principles

| Principle | Description |
|-----------|-------------|
| **Clarity over completeness** | Show what matters, hide what doesn't |
| **Continuity over transactions** | Support the therapeutic relationship across time |
| **Calm workflows over feature density** | Fewer features, better experience |
| **Desktop-first, keyboard-friendly** | Optimized for focused desktop work |
| **Local-first data storage** | User owns and controls their data |

> Praxis acts as the clinician's **working memory**, not the system of record.

---

## What Praxis Is NOT

Praxis is explicitly **NOT**:

- ❌ An Electronic Health Record (EHR)
- ❌ A billing or invoicing system
- ❌ An insurance or claims management platform
- ❌ A scheduling or calendar system
- ❌ A telehealth or video platform
- ❌ A client portal
- ❌ A practice marketing tool
- ❌ A data-harvesting or analytics product

### Praxis Does Not:

- Submit claims
- Manage insurance codes
- Replace existing EHR systems
- Store data in the cloud by default
- Force rigid or compliance-driven workflows
- Optimize for administrative staff over clinicians

> ⚠️ Any feature that introduces billing, insurance, scheduling, portals, or payer workflows is **out of scope** unless explicitly requested later.

---

## Positioning & Intent

Praxis is designed to **coexist** with existing systems such as:

- SimplePractice
- TherapyNotes
- Jane
- TheraNest

### Complementary Role

Praxis supports what those systems don't prioritize:

| Area | Praxis Role |
|------|-------------|
| **Clinical thinking** | Private space for reflection and formulation |
| **Session-to-session continuity** | Easy recall of what matters for each client |
| **Long-term outcome awareness** | Track themes and progress over months/years |

Praxis intentionally avoids competing in areas where existing systems are already bloated or compliance-driven.

---

## Implementation Guidance

When generating code, UI, documentation, or suggestions:

### Technical Preferences

- ✅ Prefer simple, explicit models over abstract frameworks
- ✅ Favor local storage (e.g., SQLite) and clear data ownership
- ✅ Optimize for performance and low cognitive friction
- ✅ Design for solo use first, small groups second

### UX Preferences

- ✅ Assume clinicians value privacy, predictability, and calm
- ✅ Keyboard-friendly interactions
- ✅ Minimal chrome, maximum content
- ✅ Quiet color palette, no aggressive notifications

### Scope Enforcement

- ❌ Avoid features that imply billing, scheduling, or insurance
- ❌ No cloud-first architecture
- ❌ No compliance-driven workflow rigidity

> 🚨 **If a requested feature conflicts with the "What Praxis Is Not" section, call it out explicitly before proceeding.**

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| **UI Framework** | WPF (.NET) |
| **Language** | C# |
| **Data Storage** | SQLite (local-first) |
| **Platform** | Windows Desktop |

---

## Design Philosophy

```
┌─────────────────────────────────────────────────────────┐
│                                                         │
│   "A calm tool for the quiet work of helping people."   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

Praxis exists because clinicians deserve software that respects their work—software that doesn't demand attention, doesn't chase metrics, and doesn't treat therapy like a transaction.

---

*© Prickly Cactus Software*
