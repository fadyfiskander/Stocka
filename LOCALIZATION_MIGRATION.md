# Localization Migration — Stocka.MVC

Refactor: remove the dictionary-based localization (`S`, `L`, `Loc.T`, `Loc.F`, local `T()` view helpers)
and write Arabic/English text inline next to where it is rendered, using `Loc.IsEn ? "en" : "ar"`.

- Source dictionary: `src/Hosts/Stocka.MVC/Localization/S.cs`
- Dictionary export: `localization-export.csv` (columns `key,ar,en`) — **356 keys**
- Every key has both `ar` and `en`; **no missing translations**.

## Decision summary (already fixed, not re-decided)

1. Language source stays the static `Loc` class. Keep `Language`, `IsRtl`, `OtherLanguage`,
   `OtherLanguageLabel`, the language-switch flow (`LocalizationController.SetLanguage`, cookie,
   returnUrl) and ASP.NET `AddLocalization()` / `UseRequestLocalization` (the switch depends on them).
2. Add exactly one member: `Loc.IsEn` → true only when the current UI culture is English
   (`Loc.Language == "en"`). Everything else (incl. missing/unknown) → Arabic.
3. `<script id="stocka-i18n">` JSON keeps the SAME property names; only the values change to inline
   `Loc.IsEn ? "..." : "..."`. `app.js` is not edited.
4. Server-side flash / `ModelState` messages → inline `Loc.IsEn ? "..." : "..."` in place.
5. DataAnnotations with old keys/resources are NOT redesigned — listed under
   "NEEDS HUMAN DECISION" and left compiling.

## Language mechanics (recon)

- `Loc.Language` = `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName` → `"ar"` or `"en"`.
- `Loc.IsRtl` = culture is RTL.
- `Loc.OtherLanguage` = the other two-letter code; `Loc.OtherLanguageLabel` = `"EN"` / `"عربي"`.
- Default culture is `ar` (`Program.cs` `DefaultRequestCulture`), supported `ar`, `en`.
- Switch: `GET /Localization/SetLanguage?culture=..&returnUrl=..` sets the request-culture cookie.
- `IStringLocalizer` / `IViewLocalizer` / `IHtmlLocalizer` are **not used anywhere**.
  Only `AddLocalization()` and `UseRequestLocalization()` exist and are **kept** (the cookie switch needs them).

---

## Files to migrate, by Area

Counts = `Loc.T(` / `Loc.F(` / local `T(` helper calls per file (Phase 0 baseline).

### Area: Shared  (`Views/`, `Views/Shared/`)
| File | Loc.T | Loc.F | local T( |
|---|---|---|---|
| `Views/Shared/_Layout.cshtml` | 1 | 0 | 41 |
| `Views/Shared/_Flash.cshtml` | 1 | 0 | 0 |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | 0 | 0 | 0 (no strings) |
| `Views/Shared/Error.cshtml` | 2 | 0 | 7 |
| `Views/Home/Index.cshtml` | 2 | 0 | 8 |
| `Views/Home/Privacy.cshtml` | 2 | 0 | 2 |

### Area: Account  (`Areas/Account/`)
| File | Loc.T | Loc.F | local T( |
|---|---|---|---|
| `Areas/Account/Controllers/AccountController.cs` | 2 | 0 | 0 |
| `Areas/Account/Views/Shared/_Layout.cshtml` | 12 | 0 | 0 |
| `Areas/Account/Views/Account/Login.cshtml` | 2 | 0 | 17 |
| `Areas/Account/Views/Account/AccessDenied.cshtml` | 2 | 0 | 3 |

### Area: Catalog  (`Areas/Catalog/`)
| File | Loc.T | Loc.F | local T( |
|---|---|---|---|
| `Areas/Catalog/Controllers/ProductsController.cs` | 12 | 4 | 0 |
| `Areas/Catalog/Controllers/CategoriesController.cs` | 11 | 4 | 0 |
| `Areas/Catalog/Views/Home/Index.cshtml` | 2 | 0 | 6 |
| `Areas/Catalog/Views/Products/Index.cshtml` | 2 | 0 | 17 |
| `Areas/Catalog/Views/Products/Create.cshtml` | 2 | 0 | 14 |
| `Areas/Catalog/Views/Products/Edit.cshtml` | 2 | 0 | 16 |
| `Areas/Catalog/Views/Products/Details.cshtml` | 2 | 0 | 17 |
| `Areas/Catalog/Views/Products/SearchByName.cshtml` | 2 | 0 | 15 |
| `Areas/Catalog/Views/Categories/Index.cshtml` | 2 | 0 | 16 |
| `Areas/Catalog/Views/Categories/Create.cshtml` | 2 | 0 | 5 |
| `Areas/Catalog/Views/Categories/Edit.cshtml` | 2 | 0 | 7 |
| `Areas/Catalog/Views/Categories/Details.cshtml` | 2 | 0 | 12 |
| `Areas/Catalog/Views/Categories/SearchByName.cshtml` | 2 | 0 | 15 |

### Area: Inventory  (`Areas/Inventory/`)
| File | Loc.T | Loc.F | local T( |
|---|---|---|---|
| `Areas/Inventory/Controllers/BatchesController.cs` | 6 | 0 | 0 |
| `Areas/Inventory/Controllers/ReportsController.cs` | 5 | 0 | 0 |
| `Areas/Inventory/Controllers/StockController.cs` | 2 | 7 | 0 |
| `Areas/Inventory/Controllers/WarehousesController.cs` | 7 | 4 | 0 |
| `Areas/Inventory/Views/Batches/Index.cshtml` | 2 | 1 | 21 |
| `Areas/Inventory/Views/Batches/Create.cshtml` | 2 | 0 | 8 |
| `Areas/Inventory/Views/Batches/Details.cshtml` | 3 | 1 | 70 |
| `Areas/Inventory/Views/Stock/Movements.cshtml` | 3 | 1 | 20 |
| `Areas/Inventory/Views/Warehouses/Index.cshtml` | 2 | 2 | 24 |
| `Areas/Inventory/Views/Warehouses/Create.cshtml` | 2 | 0 | 10 |
| `Areas/Inventory/Views/Warehouses/Edit.cshtml` | 2 | 0 | 10 |
| `Areas/Inventory/Views/Warehouses/Details.cshtml` | 2 | 3 | 24 |
| `Areas/Inventory/Views/Warehouses/SearchByName.cshtml` | 2 | 0 | 11 |
| `Areas/Inventory/Views/Reports/Index.cshtml` | 3 | 3 | 19 |
| `Areas/Inventory/Views/Reports/Balance.cshtml` | 2 | 1 | 13 |
| `Areas/Inventory/Views/Reports/Movements.cshtml` | 3 | 1 | 28 |
| `Areas/Inventory/Views/Reports/Products.cshtml` | 2 | 1 | 11 |
| `Areas/Inventory/Views/Reports/Warehouses.cshtml` | 2 | 4 | 28 |

### Support files to delete in the final phase
- `Localization/S.cs` (dictionary)
- `Localization/L.cs` (record)
- `Localization/Loc.cs` — keep `Language`, `IsRtl`, `IsEn`, `OtherLanguage`, `OtherLanguageLabel`; delete `T`, `F`.

---

## Dynamic keys (no literal key at the call site)

| Location | Pattern | Migration |
|---|---|---|
| `Areas/Inventory/Views/Reports/Index.cshtml:6` | `MoveType(int)` → `Loc.T(t switch { 0=>"Move_PurchaseIn", ... })` | inline `switch` expression, both languages per arm |
| `Areas/Inventory/Views/Reports/Movements.cshtml:6` | same `MoveType` | inline `switch` expression |
| `Areas/Inventory/Views/Stock/Movements.cshtml:6` | same `MoveType` | inline `switch` expression |
| `Areas/Inventory/Views/Batches/Details.cshtml:9-17` | local `T(key, ar, en)` fallback + `MoveType` | inline; drop the fallback helper |
| `Areas/Inventory/Views/Batches/Details.cshtml:50,95,159,227,423` | `T("Key", "ar", "en")` — key **missing** from dictionary (`Common_Select`, `Batches_Actions`, `Batches_Transfer`, `Batches_Adjust`) | use the explicit ar/en literal already at the call site (that is the source text) |

Missing-from-dictionary keys (only used with explicit fallback literals in `Batches/Details.cshtml`):
`Common_Select`, `Batches_Actions`, `Batches_Transfer`, `Batches_Adjust` — no CSV row exists, so the
inline text comes from the literal arguments in that view (`اختر…`/`Select…`, `العمليات`/`Operations`,
`تحويل`/`Transfer`, `حفظ الجرد`/`Save count`). No invented translation.

Dictionary keys never referenced by any literal call (used only inside `MoveType` switches):
`Move_PurchaseIn`, `Move_SaleOut`, `Move_TransferIn`, `Move_TransferOut`, `Move_AdjustmentIn`,
`Move_AdjustmentOut`, `Move_OrderReturn`. Also unused: `Common_CreatedAt`, `Common_Delete`.
They are still migrated where used via `MoveType`; the truly unused ones simply disappear with `S.cs`.

---

## Keys used in code-behind (controllers)

All in `Loc.T(...)` / `Loc.F(...)` form — migrate inline in the same statement.

- `Areas/Account/Controllers/AccountController.cs`: `Login_InvalidCredentials`, `Denied_Title`
- `Areas/Catalog/Controllers/ProductsController.cs`: `Products_Title`, `Products_DetailsTitle`,
  `Msg_ProductUnavailable`, `Msg_ProductUnavailableTitle`, `Products_SearchTitle`, `Products_CreateTitle`,
  `Msg_ProductCreateFailed`, `Msg_ProductCreated`, `Products_EditTitle`, `Msg_ProductNotFound`,
  `Msg_NotFoundTitle`, `Msg_ProductUpdated`, `Msg_ProductIdRequired`, `Msg_ProductActivated`,
  `Msg_ProductDeactivated`
- `Areas/Catalog/Controllers/CategoriesController.cs`: `Categories_Title`, `Msg_CategoryIdRequired`,
  `Categories_DetailsTitle`, `Categories_SearchTitle`, `Categories_CreateTitle`, `Msg_CategoryCreateFailed`,
  `Msg_CategoryCreated`, `Categories_EditTitle`, `Msg_CategoryUpdated`, `Msg_CategoryActivated`,
  `Msg_CategoryDeactivated`, `Msg_GenericError`
- `Areas/Inventory/Controllers/BatchesController.cs`: `Batches_Title`, `Batches_Register`,
  `Msg_BatchRegistered`, `Msg_BatchIdRequired`, `Batches_DetailsTitle`
- `Areas/Inventory/Controllers/ReportsController.cs`: `Reports_Title`, `ReportWarehouses_Title`,
  `ReportProducts_Title`, `ReportMovements_Title`, `Balance_Title`
- `Areas/Inventory/Controllers/StockController.cs`: `StockMovements_Title`, `Msg_Allocated`,
  `Msg_SaleRecorded`, `Msg_TransferCompleted`, `Msg_AdjustNoChange`, `Msg_AdjustRecorded`,
  `Msg_DamageRecorded`, `Msg_ReturnRecorded`, `Msg_ReversalAppended`
- `Areas/Inventory/Controllers/WarehousesController.cs`: `Warehouses_Title`, `Msg_WarehouseIdRequired`,
  `Warehouses_DetailsTitle`, `Warehouses_CreateTitle`, `Msg_WarehouseRegistered`, `Warehouses_EditTitle`,
  `Msg_WarehouseUpdated`, `Msg_WarehouseActivated`, `Msg_WarehouseDeactivated`, `Warehouses_SearchTitle`

Tag helpers / view components: none (only the built-in MVC tag helpers are registered).

---

## NEEDS HUMAN DECISION

DataAnnotations that carry validation semantics but currently use **no** custom message (so no old key
to migrate) — left untouched, compiling:

| File | Line | Attribute | Current message |
|---|---|---|---|
| `Areas/Account/Models/LoginViewModel.cs` | 7 | `[Required]` | (default framework message) |
| `Areas/Account/Models/LoginViewModel.cs` | 8 | `[EmailAddress]` | (default framework message) |
| `Areas/Account/Models/LoginViewModel.cs` | 11 | `[Required]` | (default framework message) |
| `Areas/Account/Models/LoginViewModel.cs` | 12 | `[DataType(DataType.Password)]` | (no message) |

Note: none of these reference the `S` dictionary or a `.resx`; their default messages are produced by
the framework and are **not localized** by the old system either. The client-side validator in
`wwwroot/js/app.js` replaces the required message at runtime from the `stocka-i18n` JSON. Decision 5
says: do not redesign — list and leave compiling. If you later want localized `[Required]` messages,
that is a separate task (would need per-request message resolution, which DataAnnotations do not support
natively).

No other `.resx` files exist in `Stocka.MVC`.

---

## QUESTIONS

- (none)

---

## Final results

- `dotnet build Stocka.slnx` → **Build succeeded, 0 warnings, 0 errors** (Razor views *are*
  compile-validated by the build; verified by injecting a deliberate Razor syntax error, which failed
  the build with `RZ1027`/`CS1525`, then reverting).
- Repo-wide search `Loc.T`, `Loc.F`, `.T(`, `IStringLocalizer` → **no matches** (only prose in these notes).
- Every CSV key searched as a quoted string → **no hits** anywhere in `src`.
- `== "eng"` / `== "en"` / `== "ar"` in views → **none**; the only language comparison is
  `Loc.IsEn => Language == "en"` inside `Loc.cs`.
- `Loc.IsEn ?` empty-branch scan (`: ""` / `? ""`) → **0 problems** across 673 `Loc.IsEn` occurrences.
- Every inline `(Loc.IsEn ? "EN" : "AR")` pair matches a CSV row (or the 4 documented fallbacks) —
  verified programmatically.
- Old system removed: `Localization/S.cs`, `Localization/L.cs` deleted; `Loc.T` / `Loc.F` removed.
  Kept `Language`, `IsRtl`, `IsEn`, `OtherLanguage`, `OtherLanguageLabel`, plus
  `AddLocalization()` / `UseRequestLocalization` and the switch flow.
- `wwwroot/js/app.js` untouched; `#stocka-i18n` property names unchanged.

## Progress checklist

- [x] Phase 0 — Recon, CSV export (356 keys), this file
- [x] Phase 1 — Add `Loc.IsEn`
- [x] Phase 2 — Shared area
- [x] Phase 3 — Account area
- [x] Phase 4 — Catalog area
- [x] Phase 5 — Inventory area
- [x] Phase 6 — Final: delete `T`, `F`, `S`, `L`, helpers
