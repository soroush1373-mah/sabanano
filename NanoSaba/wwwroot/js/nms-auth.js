/*--------------------------------------------------------------
# Login and sign-up without a page reload
# Every form inside [data-auth] is sent with fetch. The controller
# answers with JSON: { success, message, step, mobileNumber }.
--------------------------------------------------------------*/
(function () {
    "use strict";

    // Persian and Arabic digits typed on a phone keyboard become 0-9
    function toEnglishDigits(value) {
        return value
            .replace(/[۰-۹]/g, function (d) { return d.charCodeAt(0) - 1776; })
            .replace(/[٠-٩]/g, function (d) { return d.charCodeAt(0) - 1632; });
    }

    function initAuth(root) {
        var panels = root.querySelectorAll("[data-auth-panel]");
        var tabs = root.querySelectorAll(".nms-auth__tab");

        function form(name) {
            return root.querySelector('[data-auth-form="' + name + '"]');
        }

        function showPanel(name) {
            panels.forEach(function (p) { p.hidden = p.dataset.authPanel !== name; });
            root.querySelector("[data-auth-ok]").hidden = true;
        }

        // "userpass", "mobile" or "code" inside the login box
        function showLoginForm(name) {
            ["userpass", "mobile", "code"].forEach(function (f) { form(f).hidden = f !== name; });
            if (name === "code") root.querySelector("[data-auth-ok]").hidden = true;
            var activeTab = name === "code" ? "mobile" : name;
            tabs.forEach(function (t) { t.classList.toggle("is-active", t.dataset.authTab === activeTab); });
            var input = form(name).querySelector("input:not([type=hidden])");
            if (input) input.focus();
        }

        function setError(f, text) {
            f.querySelector("[data-auth-error]").textContent = text || "";
        }

        function validate(f) {
            var fields = f.querySelectorAll("input[required]");
            for (var i = 0; i < fields.length; i++) {
                var input = fields[i];
                var label = input.closest("label").querySelector("span").textContent;
                input.value = input.value.trim();
                if (input.inputMode === "numeric") input.value = toEnglishDigits(input.value);

                if (!input.value) return { input: input, text: "وارد کردن " + label + " الزامی است" };
                if (input.maxLength === 11 && !/^09\d{9}$/.test(input.value))
                    return { input: input, text: label + " باید ۱۱ رقم و با ۰۹ شروع شود" };
                if (input.type === "email" && !/^\S+@\S+\.\S+$/.test(input.value))
                    return { input: input, text: label + " درست نیست" };
                if (input.name === "FullName" && input.value.indexOf(" ") < 0)
                    return { input: input, text: "لطفا نام و نام خانوادگی را با فاصله وارد کنید" };
            }
            return null;
        }

        async function send(f) {
            var problem = validate(f);
            if (problem) {
                setError(f, problem.text);
                problem.input.focus();
                return;
            }
            setError(f, "");

            var button = f.querySelector("[type=submit]");
            button.disabled = true;
            button.classList.add("is-loading");
            try {
                var response = await fetch(f.action, {
                    method: "POST",
                    body: new FormData(f),
                    headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" },
                    credentials: "same-origin"
                });
                var data = await response.json();
                handle(f, data);
            } catch (e) {
                setError(f, "ارتباط با سرور برقرار نشد. دوباره تلاش کنید.");
            } finally {
                button.disabled = false;
                button.classList.remove("is-loading");
            }
        }

        function handle(f, data) {
            if (!data.success) {
                setError(f, data.message || "خطایی رخ داد. دوباره تلاش کنید.");
                return;
            }

            switch (data.step) {
                case "code":
                    root.querySelector("[data-auth-mobile]").textContent = data.mobileNumber;
                    form("code").reset();
                    setError(form("code"), "");
                    showLoginForm("code");
                    break;
                case "registered":
                    // Account is ready: open the mobile login with the number filled in
                    form("register").reset();
                    showPanel("login");
                    history.replaceState(null, "", location.pathname);
                    form("mobile").querySelector("input[name=MobileNumber]").value = data.mobileNumber || "";
                    showLoginForm("mobile");
                    setError(form("mobile"), "");
                    var ok = root.querySelector("[data-auth-ok]");
                    ok.textContent = data.message;
                    ok.hidden = false;
                    break;
                case "signedIn":
                    // The header was drawn before login, so load the page once to show the user's menu
                    var page = root.closest("[data-auth-redirect]");
                    if (page) window.location.href = page.dataset.authRedirect;
                    else window.location.reload();
                    break;
            }
        }

        root.addEventListener("submit", function (e) {
            if (!e.target.matches("[data-auth-form]")) return;
            e.preventDefault();
            send(e.target);
        });

        root.addEventListener("click", function (e) {
            var tab = e.target.closest("[data-auth-tab]");
            if (tab) showLoginForm(tab.dataset.authTab);

            var show = e.target.closest("[data-auth-show]");
            if (show) {
                showPanel(show.dataset.authShow);
                history.pushState(null, "", show.dataset.authShow === "register" ? "?mode=register" : location.pathname);
                var first = root.querySelector('[data-auth-panel="' + show.dataset.authShow + '"] form:not([hidden]) input:not([type=hidden])');
                if (first) first.focus();
            }
        });

        // The address shows which box is open (/Home/SignIn or /Home/SignIn?mode=register),
        // so the browser back button and a shared link open the same box
        window.addEventListener("popstate", function () {
            showPanel(new URLSearchParams(location.search).get("mode") === "register" ? "register" : "login");
        });
    }

    document.querySelectorAll("[data-auth]").forEach(initAuth);
})();
