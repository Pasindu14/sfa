# CLAUDE.md — sfa_mobile (Flutter)

## How to Run

```bash
cd sfa_mobile
flutter pub get
flutter run
flutter test

# Regenerate DI code after adding @injectable annotations
dart run build_runner build --delete-conflicting-outputs
```

---

## Directory Layout

```
sfa_mobile/lib/
├── main.dart                  ← entry point, GetIt setup
├── core/
│   ├── connectivity/          ← offline detection (connectivity_plus)
│   ├── constants/             ← app-wide constants
│   ├── db/                    ← SQLite via sqflite (database_helper.dart)
│   ├── device/                ← device info utilities
│   ├── di/                    ← get_it + injectable (injection.dart + injection.config.dart)
│   ├── env/                   ← app_env.dart — API base URL from environment
│   ├── errors/                ← failure types
│   ├── network/               ← Dio client, TokenInterceptor, api_response, token_cache
│   ├── router/                ← go_router (app_router.dart, go_router_refresh_stream.dart)
│   ├── sync/                  ← bill_sync_service, not_billing_sync_service
│   ├── theme/                 ← app theme
│   └── utils/                 ← shared utilities
└── features/{feature}/
    ├── data/
    │   ├── datasources/       ← remote (Dio) + local (SQLite)
    │   ├── models/            ← JSON-serializable models (fromJson/toJson)
    │   └── repositories/      ← implements domain repository contracts
    ├── domain/
    │   ├── entities/          ← pure Dart domain models (no JSON)
    │   ├── repositories/      ← abstract repository contracts
    │   └── usecases/          ← single-responsibility use cases
    └── presentation/
        ├── bloc/              ← BLoC + events + states
        ├── pages/             ← full-screen pages
        └── widgets/           ← feature-specific widgets
```

---

## Implemented Features

| Feature | Description |
|---------|-------------|
| auth | Login, secure token storage, session management |
| splash | Splash screen + auth check on launch |
| outlets | Outlet list and detail view |
| products | Product catalog (prices live on the product) |
| bills | Bill creation and management |
| not_billings | Not-billing reason recording |
| outlet_bill_history | Bill history per outlet |
| create_outlet | New outlet creation workflow |
| routes | Route list and navigation |
| route_assignment | Daily route assignment |
| sales_rep | Sales rep profile |
| rep_assignment | Rep-to-route assignment |
| supervisor | Supervisor dashboard |
| sync | Offline data sync with backend |
| debug | Debug tools (dev only) |

---

## Key Conventions

- **State management:** BLoC (`flutter_bloc`) with `bloc_concurrency` for event transformers
- **Navigation:** `go_router` — all routes defined in `core/router/app_router.dart`; use `GoRouter.of(context).go()` for navigation
- **DI:** `get_it` + `injectable` — annotate services with `@injectable`, `@lazySingleton`, or `@singleton`; run build_runner after changes
- **HTTP:** `Dio` client via `core/network/dio_client.dart`; `TokenInterceptor` auto-attaches Bearer JWT and handles 401 refresh
- **Offline storage:** `sqflite` through `DatabaseHelper` singleton — never access DB directly from BLoC
- **Auth tokens:** `flutter_secure_storage` only — never `SharedPreferences`
- **API base URL:** Read from `AppEnv` (`core/env/app_env.dart`) — never hardcode

## Architecture Flow

```
Page → BLoC (event) → UseCase → Repository → DataSource (remote/local)
```

- BLoC emits states; pages rebuild via `BlocBuilder` / `BlocConsumer`
- Use cases are single-method classes — one responsibility each
- Repository decides remote vs. local based on connectivity

---

## Never Do

- Never store JWT in `SharedPreferences` — use `flutter_secure_storage`
- Never hardcode API base URL — use `AppEnv`
- Never call Dio directly from BLoC — go through use case → repository → datasource
- Never call hard-delete API endpoints — use soft-delete
- Never send tenant/company ID from the client — backend resolves from JWT
