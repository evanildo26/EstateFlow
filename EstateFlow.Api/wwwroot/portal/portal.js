// Executor / heir progress portal. Plain JavaScript, no external libraries.
// All server text is inserted with textContent, never innerHTML.
(function () {
    "use strict";

    const TOKEN_KEY = "estateflow.portal.token";
    const PORTAL_ROLES = ["Executor", "Heir"];

    const views = ["set-password-view", "login-view", "code-view", "estates-view"];
    const $ = (id) => document.getElementById(id);

    let twoFactorToken = null;

    function show(viewId) {
        views.forEach((v) => $(v).classList.toggle("hidden", v !== viewId));
    }

    function say(text, isError) {
        const box = $("message");
        box.textContent = text || "";
        box.classList.toggle("hidden", !text);
        box.classList.toggle("error", !!isError);
    }

    function getToken() {
        try { return sessionStorage.getItem(TOKEN_KEY); } catch { return null; }
    }

    function setToken(token) {
        try {
            if (token) sessionStorage.setItem(TOKEN_KEY, token);
            else sessionStorage.removeItem(TOKEN_KEY);
        } catch { /* storage unavailable: token lives only for this page */ }
    }

    async function api(path, options) {
        const headers = { "Content-Type": "application/json" };
        const token = getToken();
        if (token) headers.Authorization = "Bearer " + token;

        const response = await fetch(path, { ...options, headers });
        let body = null;
        try { body = await response.json(); } catch { /* empty body */ }
        return { ok: response.ok, status: response.status, body };
    }

    function errorText(result, fallback) {
        if (result.status === 429) return "Too many attempts. Please wait a minute and try again.";
        if (result.status === 423) return "This account is temporarily locked after too many failed attempts. Try again in 15 minutes.";
        return (result.body && (result.body.message || (result.body.errors && result.body.errors.join(" ")))) || fallback;
    }

    function withBusy(form, work) {
        return async (event) => {
            event.preventDefault();
            const button = form.querySelector("button");
            button.disabled = true;
            try { await work(); }
            catch { say("Could not reach EstateFlow. Check your connection and try again.", true); }
            finally { button.disabled = false; }
        };
    }

    // ---------- Set password (invitation link) ----------

    function startSetPassword(params) {
        $("sp-email").value = params.get("email") || "";
        show("set-password-view");

        const form = $("set-password-form");
        form.addEventListener("submit", withBusy(form, async () => {
            const password = $("sp-password").value;
            if (password.length < 8) return say("Please choose a password of at least 8 characters.", true);
            if (password !== $("sp-confirm").value) return say("The two passwords do not match.", true);

            const result = await api("/api/auth/portal/set-password", {
                method: "POST",
                body: JSON.stringify({ email: params.get("email"), token: params.get("token"), newPassword: password })
            });

            if (!result.ok) return say(errorText(result, "The password could not be set."), true);

            history.replaceState(null, "", location.pathname);
            $("login-email").value = params.get("email") || "";
            say("Your password has been set. Please sign in.");
            show("login-view");
        }));
    }

    // ---------- Sign in ----------

    async function completeSignIn(body) {
        if (!body || !PORTAL_ROLES.includes(body.role)) {
            say("This portal is for executors and heirs. Staff should use the EstateFlow desktop app.", true);
            show("login-view");
            return;
        }

        setToken(body.token);
        say("");
        await loadEstates();
    }

    const loginForm = $("login-form");
    loginForm.addEventListener("submit", withBusy(loginForm, async () => {
        const result = await api("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ email: $("login-email").value.trim(), password: $("login-password").value })
        });

        $("login-password").value = "";

        if (!result.ok) return say(errorText(result, "Invalid email or password."), true);

        if (result.body.requiresTwoFactor) {
            twoFactorToken = result.body.twoFactorToken;
            say("");
            show("code-view");
            $("code").focus();
            return;
        }

        await completeSignIn(result.body);
    }));

    const codeForm = $("code-form");
    codeForm.addEventListener("submit", withBusy(codeForm, async () => {
        const result = await api("/api/auth/login/verify-2fa", {
            method: "POST",
            body: JSON.stringify({ twoFactorToken, code: $("code").value.trim() })
        });

        $("code").value = "";

        if (!result.ok) {
            if (result.status === 423 || (result.body && /expired/i.test(result.body.message || ""))) {
                twoFactorToken = null;
                show("login-view");
            }
            return say(errorText(result, "Invalid authentication code."), true);
        }

        twoFactorToken = null;
        await completeSignIn(result.body);
    }));

    $("sign-out").addEventListener("click", () => {
        setToken(null);
        $("estates").replaceChildren();
        say("You have been signed out.");
        show("login-view");
    });

    // ---------- Estates ----------

    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function formatDate(value) {
        if (!value) return "";
        return new Date(value).toLocaleDateString("en-ZA", { day: "numeric", month: "long", year: "numeric" });
    }

    function renderEstate(estate) {
        const card = el("article", "card");

        const head = el("div", "estate-head");
        const titleBlock = el("div");
        titleBlock.append(
            el("h2", null, "Estate late " + estate.deceasedFullName),
            el("div", "meta", "File " + estate.fileNumber + " · You are listed as: " + estate.yourRole));
        head.append(titleBlock, el("span", "badge", estate.estateStatus));
        card.append(head);

        const bar = el("div", "progress");
        bar.setAttribute("role", "progressbar");
        bar.setAttribute("aria-valuemin", "0");
        bar.setAttribute("aria-valuemax", "100");
        bar.setAttribute("aria-valuenow", String(estate.percentComplete));
        const fill = el("span");
        fill.style.width = Math.max(0, Math.min(100, estate.percentComplete)) + "%";
        bar.append(fill);
        card.append(bar);

        const next = el("div", "next");
        if (estate.nextStep) {
            next.append(document.createTextNode(estate.percentComplete + "% complete · Next step: "), el("strong", null, estate.nextStep));
        } else {
            next.textContent = "This estate has been finalised.";
        }
        card.append(next);

        card.append(el("h3", null, "Stages"));
        const steps = el("ol", "steps");
        estate.milestones.forEach((m) => {
            const state = m.completedOn ? "done" : (m.isNext ? "current" : "pending");
            const item = el("li", state);
            const text = el("div");
            text.append(el("div", "title", m.title), el("div", "desc", m.description));
            item.append(
                el("span", "dot"),
                text,
                el("span", "when", m.completedOn ? formatDate(m.completedOn) : (m.isNext ? "In progress" : "")));
            steps.append(item);
        });
        card.append(steps);

        if (estate.recentUpdates.length > 0) {
            card.append(el("h3", null, "Recent updates"));
            const updates = el("ul", "updates");
            estate.recentUpdates.forEach((u) => {
                const item = el("li");
                const time = el("time", null, formatDate(u.date));
                time.dateTime = u.date;
                item.append(time, document.createTextNode(u.text));
                updates.append(item);
            });
            card.append(updates);
        }

        if (estate.controllerName) {
            const contact = el("p", "contact", "Questions? Contact your estate controller, " + estate.controllerName);
            if (estate.controllerEmail) {
                contact.append(document.createTextNode(", at "));
                const link = el("a", null, estate.controllerEmail);
                link.href = "mailto:" + estate.controllerEmail;
                contact.append(link);
            }
            contact.append(document.createTextNode("."));
            card.append(contact);
        }

        return card;
    }

    async function loadEstates() {
        let result;
        try {
            result = await api("/api/portal/estates", { method: "GET" });
        } catch {
            say("Could not reach EstateFlow. Check your connection and try again.", true);
            return;
        }

        if (result.status === 401 || result.status === 403) {
            setToken(null);
            say(result.status === 403 ? "This portal is for executors and heirs." : "Your session has ended. Please sign in again.", result.status === 403);
            show("login-view");
            return;
        }

        if (!result.ok) {
            say("Your estates could not be loaded. Please try again shortly.", true);
            return;
        }

        const list = $("estates");
        list.replaceChildren();

        if (result.body.length === 0) {
            list.append(el("p", "card muted", "No estates are linked to your email address yet. Please contact your estate controller."));
        } else {
            result.body.forEach((estate) => list.append(renderEstate(estate)));
        }

        show("estates-view");
    }

    // ---------- Start ----------

    if (location.hash.startsWith("#set-password")) {
        const query = location.hash.slice("#set-password".length).replace(/^\?/, "");
        startSetPassword(new URLSearchParams(query));
    } else if (getToken()) {
        loadEstates();
    } else {
        show("login-view");
    }
})();
