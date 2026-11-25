/* ============================================================
   Stocka — app.js
   Zero dependencies. Small, explicit, comment-anchored modules:
   1) Theme        — light/dark, persisted, respects system choice
   2) Sidebar      — drawer (mobile), rail collapse (desktop)
   3) Nav groups   — open the group whose section is active
   4) Alerts       — dismissible flash override data-alert-dismiss
   5) Validator    — replaces jQuery.validate: reads data-val-*
                     attributes emitted by ASP.NET + HTML attributes,
                     shows localized messages, blocks double submits
   6) File inputs  — selected filename inside the dropzone

   Conventions: every behavior hooks data-* hooks, never classes.
   ============================================================ */
(() => {
    "use strict";

    const ready = (fn) =>
        document.readyState !== "loading"
            ? fn()
            : document.addEventListener("DOMContentLoaded", fn);

    /* NOTE: init runs at the very bottom of this IIFE — the modules below are
       `const` declarations, and calling them here would hit the temporal dead
       zone (deferred scripts already see readyState "interactive"). */

    /* ------------------------------------------------------------
       1) Theme
       ------------------------------------------------------------ */
    const Theme = {
        KEY: "stocka-theme",

        init() {
            const btn = document.querySelector("[data-theme-toggle]");
            if (!btn) return;

            btn.addEventListener("click", () => {
                const next = this.current() === "dark" ? "light" : "dark";
                this.apply(next);
                try { localStorage.setItem(this.KEY, next); } catch { /* private mode */ }
            });

            // keep other tabs in sync
            window.addEventListener("storage", (e) => {
                if (e.key === this.KEY && e.newValue) this.apply(e.newValue, false);
            });
        },

        current() {
            try { return localStorage.getItem(this.KEY) || "light"; }
            catch { return "light"; }
        },

        apply(theme) {
            const root = document.documentElement;
            root.setAttribute("data-theme", theme);
            root.style.colorScheme = theme;
            const btn = document.querySelector("[data-theme-toggle]");
            if (btn) {
                btn.setAttribute("aria-label",
                    theme === "dark" ? (btn.dataset.ariaLabelLight || "Switch to light theme")
                                     : (btn.dataset.ariaLabelDark || "Switch to dark theme"));
            }
        }
    };

    /* ------------------------------------------------------------
       2) Shell (sidebar drawer + rail)
       ------------------------------------------------------------ */
    const Shell = {
        init() {
            const shell = document.getElementById("shell");
            if (!shell) return;

            // mobile drawer
            document.querySelectorAll("[data-nav-toggle]").forEach((btn) => {
                btn.addEventListener("click", () => {
                    const open = shell.getAttribute("data-nav-open") === "true";
                    shell.setAttribute("data-nav-open", String(!open));
                });
            });

            // scrim click closes
            shell.querySelectorAll("[data-nav-close]").forEach((el) => {
                el.addEventListener("click", () => shell.setAttribute("data-nav-open", "false"));
            });

            // Escape closes drawer
            document.addEventListener("keydown", (e) => {
                if (e.key === "Escape") shell.setAttribute("data-nav-open", "false");
            });

            // desktop rail collapse (persisted)
            const railBtn = document.querySelector("[data-rail-toggle]");
            if (railBtn) {
                try {
                    if (localStorage.getItem("stocka-rail") === "1") shell.setAttribute("data-rail", "true");
                    railBtn.addEventListener("click", () => {
                        const railed = shell.getAttribute("data-rail") === "true";
                        shell.setAttribute("data-rail", String(!railed));
                        localStorage.setItem("stocka-rail", railed ? "0" : "1");
                    });
                } catch { /* storage unavailable */ }
            }
        }
    };

    /* ------------------------------------------------------------
       3) Nav groups — open the section that owns the active link
       ------------------------------------------------------------ */
    const NavGroups = {
        init() {
            document.querySelectorAll("details.nav-group").forEach((group) => {
                if (group.querySelector(".nav-link.is-active")) {
                    group.setAttribute("open", "");
                    group.classList.add("is-open");
                }
                group.addEventListener("toggle", () => {
                    group.classList.toggle("is-open", group.open);
                });
            });
        }
    };

    /* ------------------------------------------------------------
       4) Alerts — dismiss buttons
       ------------------------------------------------------------ */
    const Alerts = {
        init() {
            document.querySelectorAll("[data-alert-dismiss]").forEach((btn) => {
                btn.addEventListener("click", () => {
                    const alert = btn.closest(".alert");
                    if (!alert) return;
                    alert.style.opacity = "0";
                    alert.style.transition = "opacity 160ms ease";
                    setTimeout(() => alert.remove(), 170);
                });
            });
        }
    };

    /* ------------------------------------------------------------
       5) Validator — unobtrusive validation without any library
       ------------------------------------------------------------
       Supported sources (both are read; attribute wins when a
       custom message is present):
         HTML:    required, minlength, maxlength, min, max, type
         ASP.NET: data-val-required, data-val-length(-min/max),
                  data-val-range(-min/max), data-val-regex,
                  data-val-email, data-val-number
    ------------------------------------------------------------ */
    const Validator = {
        T: {}, // localized messages

        init() {
            const holder = document.getElementById("stocka-i18n");
            if (holder) {
                try { this.T = JSON.parse(holder.textContent); } catch { this.T = {}; }
            }

            document.querySelectorAll("form[data-validate]").forEach((form) => {
                form.setAttribute("novalidate", ""); // messages must be ours, not the browser's
                form.addEventListener("submit", (e) => this.onSubmit(e, form));
            });

            // clear + live revalidate while typing
            document.addEventListener("input", (e) => {
                if (e.target.closest?.("form[data-validate]")) this.clearControl(e.target);
            });
        },

        onSubmit(e, form) {
            const errors = this.validateForm(form);
            this.renderSummary(form, errors);

            if (errors.length > 0) {
                e.preventDefault();
                const first = form.querySelector("[aria-invalid='true']");
                first?.focus();
                first?.scrollIntoView({ block: "center", behavior: "smooth" });
                return;
            }

            // valid submit → busy state + double-submit guard
            form.querySelectorAll("button[type='submit']").forEach((b) => {
                b.classList.add("is-busy");
                b.setAttribute("disabled", "");
            });
        },

        validateForm(form) {
            const controls = form.querySelectorAll("input, select, textarea");
            const errors = [];

            controls.forEach((ctrl) => {
                const msg = this.validateControl(ctrl, form);
                this.paintControl(ctrl, msg);
                if (msg) errors.push({ ctrl, msg });
            });

            return errors;
        },

        validateControl(ctrl, form) {
            if (ctrl.disabled || ctrl.type === "hidden" || ctrl.type === "submit" || !ctrl.willValidate) return null;
            if (ctrl.offsetParent === null && ctrl.type !== "radio") return null; // display:none → not user-reachable

            const v = ctrl.checkValidity(); // native constraint check (we only suppress the UI)
            if (v) return null;

            return this.messageFor(ctrl, ctrl.validity);
        },

        messageFor(ctrl, val) {
            const T = this.T, fallback = T.required || "This field is required.";
            const label = this.labelOf(ctrl);

            if (val.valueMissing) {
                const custom = ctrl.getAttribute("data-val-required");
                return custom && custom.toLowerCase() !== "the {0} field is required." && !custom.startsWith("The ")
                    ? custom
                    : T.required || fallback;
            }
            if (val.typeMismatch)
                return ctrl.type === "email" ? (T.email || "Enter a valid email address.") : (T.url || "Enter a valid URL.");
            if (val.patternMismatch) return ctrl.getAttribute("data-val-regex") || T.pattern;
            if (val.tooShort) return fmt(T.minlength, label, ctrl.getAttribute("minlength"));
            if (val.tooLong) return fmt(T.maxlength, label, ctrl.getAttribute("maxlength"));
            if (val.rangeUnderflow) return fmt(T.min, label, ctrl.getAttribute("min"));
            if (val.rangeOverflow) return fmt(T.max, label, ctrl.getAttribute("max"));
            if (val.stepMismatch) return fmt(T.step, label, ctrl.getAttribute("step"));
            if (val.badInput) return T.number;
            return val.valid ? null : fallback;
        },

        labelOf(ctrl) {
            return ctrl.getAttribute("data-label") || ctrl.labels?.[0]?.textContent?.trim() || ctrl.name || "";
        },

        paintControl(ctrl, msg) {
            if (msg) {
                ctrl.classList.add("is-invalid");
                ctrl.setAttribute("aria-invalid", "true");
                if (ctrl.type === "radio" || ctrl.type === "checkbox") return; // summary carries these
                const node = this.errorNode(ctrl);
                if (node) node.textContent = msg;
            }
        },

        clearControl(ctrl) {
            if (!ctrl || !ctrl.closest?.("form[data-validate]")) return;
            if (!(ctrl.classList.contains("is-invalid") || ctrl.getAttribute("aria-invalid") === "true")) return;

            const msg = ctrl.willValidate && !ctrl.checkValidity() ? this.messageFor(ctrl, ctrl.validity) : null;
            ctrl.classList.toggle("is-invalid", !!msg);
            const node = this.errorNode(ctrl);

            if (msg) {
                ctrl.setAttribute("aria-invalid", "true");
                if (node) node.textContent = msg;
            } else {
                ctrl.removeAttribute("aria-invalid");
                if (node) node.textContent = "";
            }
        },

        errorNode(ctrl) {
            if (ctrl.type === "radio" || ctrl.type === "checkbox") {
                // shared field message: [data-error-for="PropertyName"]
                return ctrl.form?.querySelector(`[data-error-for="${ctrl.name}"]`) || null;
            }
            // prefer an existing sibling <span asp-validation-for> (.field-error) inside the same field
            return ctrl.closest(".field")?.querySelector(":scope > .field-error") || null;
        },

        renderSummary(form, errors) {
            const box = form.querySelector(".form-summary");
            if (!box) return;

            if (errors.length === 0) {
                // restore any server-rendered summary content
                if (box.dataset.server) box.innerHTML = box.dataset.server;
                return;
            }

            if (!box.dataset.server) box.dataset.server = box.innerHTML; // remember server content once
            box.textContent = "";

            const text = errors.length === 1 ? this.T.oneIssue : this.T.manyIssues;
            if (text) {
                const p = document.createElement("p");
                p.textContent = fmt(text);
                p.style.fontWeight = "600";
                p.style.margin = "0 0 6px";
                box.appendChild(p);
            }
            const ul = document.createElement("ul");
            errors.slice(0, 6).forEach(({ msg }) => {
                const li = document.createElement("li");
                li.textContent = msg;
                ul.appendChild(li);
            });
            box.appendChild(ul);
        }
    };

    function fmt(t, ...args) {
        if (!t) return args.filter(Boolean).join(" ");
        let i = 0;
        return t.replace(/\{(\d+)\}/g, (m, d) => args[Number(d)] ?? m);
    }

    /* ------------------------------------------------------------
       6) File inputs — show chosen filename
       ------------------------------------------------------------ */
    const FileFields = {
        init() {
            document.querySelectorAll(".file-field input[type='file']").forEach((input) => {
                input.addEventListener("change", () => {
                    const nameEl = input.closest(".file-field").querySelector("[data-file-name]");
                    if (!nameEl) return;
                    nameEl.textContent = input.files?.length
                        ? Array.from(input.files).map((f) => f.name).join(", ")
                        : nameEl.dataset.fileHint || "";
                });
            });
        }
    };

    /* ------------------------------------------------------------
       Boot — every module above is declared; safe to initialize.
       ------------------------------------------------------------ */
    ready(() => {
        Theme.init();
        Shell.init();
        NavGroups.init();
        Alerts.init();
        Validator.init();
        FileFields.init();
    });
})();
