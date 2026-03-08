# CLAUDE.md — sfa_mobile (Flutter)

## Status

**Project not yet initialized.** The `sfa_mobile/` directory is empty — no `pubspec.yaml` or Flutter files exist yet.

This file contains placeholder conventions to be verified and expanded once the project is scaffolded.

---

## How to Run (TODO: verify once initialized)

```bash
cd sfa_mobile
flutter pub get
flutter run
```

---

## Planned Conventions (TODO: verify against actual code)

- **Target users:** Field sales reps — mobile-first, offline-capable
- **API:** Same `sfa_api` backend — Bearer JWT in `Authorization` header
- **Auth:** TODO: verify token storage (flutter_secure_storage expected)
- **State management:** TODO: verify (Riverpod / BLoC / Provider)
- **Navigation:** TODO: verify (go_router expected)
- **Structure:** TODO: document once `lib/` directory is created

---

## Never Do

- Never hardcode API base URL — use environment config
- Never store JWT tokens in SharedPreferences — use secure storage
- Never hard-delete data — call soft-delete API endpoints
- Never send tenant/company ID from the client — backend resolves from JWT
