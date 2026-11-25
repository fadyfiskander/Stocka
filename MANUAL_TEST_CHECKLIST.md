# Manual Test Checklist — Stocka.MVC localization refactor

The dictionary system (`S`, `L`, `Loc.T`, `Loc.F`, view-local `T()`) was replaced with inline
`Loc.IsEn ? "English" : "العربية"` expressions. **I cannot see the running UI**, so please run these
checks yourself. Report anything that looks wrong.

Prerequisites
- Build and run: `dotnet run --project src/Hosts/Stocka.MVC` (development opens the Photino window),
  or run the host and open the printed URL in a browser.
- Use a **fresh profile / clear cookies** before the "first load" check, so no old
  `.AspNetCore.Culture` cookie lingers.

## 1. Arabic default on first load
- [ ] With no culture cookie, the app loads in **Arabic** (default is `ar`).
- [ ] `<html>` has `lang="ar"` and `dir="rtl"`.
- [ ] Layout, sidebar, header, footer text are all Arabic (no raw keys like `Nav_Home` anywhere).

## 2. Switch to English (language switch in the header)
- [ ] Click the language button (shows **EN** while in Arabic).
- [ ] You stay on the same page (returnUrl works), not bounced to home.
- [ ] `<html>` becomes `lang="en"` and `dir="ltr"`.
- [ ] **Whole UI** flips to English:
  - [ ] Sidebar nav: Home / Catalog / Products / Categories / Inventory / Warehouses / Batches /
        Movements / Reports / Overview / Balance / Movement report / Zero stock / Warehouse reports.
  - [ ] Header search placeholder ("Search products"), language button label ("عربي"), theme button tooltip.
  - [ ] Footer note ("Business operations system").
  - [ ] Page `<title>` and `ViewData["Title"]` values.
- [ ] Sidebar/rail behavior is still correct in LTR (collapse/expand, active highlight on the correct side).

## 3. Flash messages (TempData)
Trigger each and confirm the message language follows the UI:
- [ ] Create a product → success message ("Product '…' created successfully." / Arabic equivalent).
- [ ] Update a product → success message.
- [ ] Activate / deactivate a product → messages.
- [ ] Create / update / activate / deactivate a category → messages.
- [ ] Register / update / activate / deactivate a warehouse → messages.
- [ ] Register a batch → "Batch registered. Allocate it to a warehouse…".
- [ ] Allocate stock → "Allocated. Batch now holds {n} units…" (numbers appear in the right slots).
- [ ] Record a sale → "Sale recorded ({n} units out)."
- [ ] Transfer between warehouses → "Transfer {ref} completed: {a} … {b} …".
- [ ] Adjust count with **no change** → "Physical count matches recorded quantity — no adjustment recorded."
- [ ] Adjust count with change → "Adjustment recorded ({signed n})." (sign `+`/`-` correct).
- [ ] Damage write-off → "Damage write-off recorded ({n} units out). History preserved."
- [ ] Customer return → "Customer return recorded ({n} units in)."
- [ ] Reverse a movement → "Correction movement {id} appended (original movement {id} not modified)."
- [ ] Error flashes (e.g. missing id) → localized.

## 4. Validation
- [ ] Login with empty fields → client validation messages in the current language
      (required / email / minlength / maxlength / pattern / min / max / step / number).
- [ ] Submit an invalid login → server message "Invalid email or password." / Arabic.
- [ ] The issue summary line reads "Please fix the following issue(s):" / Arabic, singular vs plural correct.
- [ ] The i18n JSON script (`#stocka-i18n`) still has the **same property names**
      (`required, email, url, pattern, minlength, maxlength, min, max, step, number, oneIssue, manyIssues`).
      (Open devtools → Elements → `#stocka-i18n`.)

## 5. Placeholders, tooltips, aria-labels
- [ ] Login: email placeholder, password placeholder.
- [ ] Search inputs placeholders.
- [ ] Product/Category/Warehouse create & edit form placeholders.
- [ ] Header search `aria-label`; sidebar `aria-label` ("Main navigation"); nav buttons `aria-label`.
- [ ] Language switch link `title` ("Switch to Arabic"/"Switch to English").
- [ ] Theme toggle `data-aria-label-light` / `data-aria-label-dark`.
- [ ] Flash close button `aria-label` ("Close" / Arabic).

## 6. Switch back to Arabic
- [ ] Click the language button (shows **عربي** while in English).
- [ ] UI returns fully to Arabic, `dir="rtl"`, same page preserved.
- [ ] Sidebar/rail RTL behavior correct (drawer from the correct side, rail flip).

## 7. Pages with dynamic text
- [ ] Pagination on Batches list, Stock movements, Warehouses list, Balance, Reports → Products:
      "Page {n} of {m}" / Arabic, plus the counts in parentheses.
- [ ] Warehouse details: "({n} current units (raw unit count))" and "({n} placements)".
- [ ] Reports overview: "({n} active)", "({n} exhausted)", "generated {date} UTC".
- [ ] Reports movements: "Date range {a} → {b}…".
- [ ] Reports warehouses: "Stock {dir}" / "Cost {dir}" toggles with ascending/descending.
- [ ] Batch details: "Max {n} unallocated units. Placement records a PurchaseIn movement."
- [ ] Movement-type labels (Purchase in / Sale out / Transfer in / Transfer out / Adjustment in /
      Adjustment out / Order return) on all four pages that render them.
- [ ] Warehouse type labels (Shop / Storage).

## 8. No leftover dictionary behavior
- [ ] Search the rendered HTML for any raw key-like token (e.g. `Nav_`, `Msg_`, `Val_`, `Common_`) —
      there should be none.
- [ ] No page shows a raw key instead of text.

## Known / by-design notes
- `[Required]` / `[EmailAddress]` in `LoginViewModel` have **no** custom message (they never used the
  dictionary). See `LOCALIZATION_MIGRATION.md` → NEEDS HUMAN DECISION.
- The generated `src/styles/tailwind.output.css` may gain a `.visible` utility because the words
  "made visible" now appear as inline text in `Login.cshtml` (Tailwind scans template text). Harmless.
