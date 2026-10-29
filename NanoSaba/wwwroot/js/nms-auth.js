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
      .replace(/[۰-۹]/g, function (d) {
        return d.charCodeAt(0) - 1776;
      })
      .replace(/[٠-٩]/g, function (d) {
        return d.charCodeAt(0) - 1632;
      });
  }

  function initAuth(root) {
    var panels = root.querySelectorAll("[data-auth-panel]");
    var tabs = root.querySelectorAll(".nms-auth__tab");

    function form(name) {
      return root.querySelector('[data-auth-form="' + name + '"]');
    }

    function showPanel(name) {
      panels.forEach(function (p) {
        p.hidden = p.dataset.authPanel !== name;
      });
      root.querySelector("[data-auth-ok]").hidden = true;
    }

    // "userpass", "mobile" or "code" inside the login box
    function showLoginForm(name) {
      ["userpass", "mobile", "code"].forEach(function (f) {
        form(f).hidden = f !== name;
      });
      if (name === "code") root.querySelector("[data-auth-ok]").hidden = true;
      var activeTab = name === "code" ? "mobile" : name;
      tabs.forEach(function (t) {
        t.classList.toggle("is-active", t.dataset.authTab === activeTab);
      });
      var input = form(name).querySelector("input:not([type=hidden])");
      if (input) input.focus();
    }

    function setError(f, text) {
      f.querySelector("[data-auth-error]").textContent = text || "";
    }
    // Sign-up rules: each one returns an error text, or "" when the value is fine
    var registerRules = {
      FullName: function (v) {
        if (!/^[؀-ۿ‌ a-zA-Z]+$/.test(v)) return "نام فقط باید شامل حروف باشد";
        if (v.split(/\s+/).length < 2)
          return "لطفا نام و نام خانوادگی را با فاصله وارد کنید";
        return "";
      },
      CompanyName: function () {
        return "";
      },
      Mobile: function (v) {
        return /^09\d{9}$/.test(v)
          ? ""
          : "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود";
      },
      Address: function (v) {
        return v.length >= 10 ? "" : "آدرس باید حداقل ۱۰ حرف باشد";
      },
      Email: function (v) {
        return /^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/.test(v)
          ? ""
          : "ایمیل درست نیست (مثال: name@site.com)";
      },
      Password: function (v) {
        if (v.length < 8) return "رمز عبور باید حداقل ۸ حرف باشد";
        if (!/[a-zA-Z]/.test(v) || !/\d/.test(v))
          return "رمز عبور باید شامل حرف انگلیسی و عدد باشد";
        return "";
      },
      ConfirmPassword: function (v, f) {
        return v === f.querySelector("[name=Password]").value
          ? ""
          : "تکرار رمز عبور با رمز عبور یکسان نیست";
      },
    };

    // Checks one sign-up field and writes its error under it. Returns true when the field is fine.
    function checkField(input) {
      var label = input.closest("label").querySelector("span").textContent;
      var v =
        input.type === "password"
          ? input.value
          : toEnglishDigits(input.value.trim());

      var text = v
        ? registerRules[input.name](v, input.form)
        : "وارد کردن " + label + " الزامی است";
      input.closest("label").querySelector("[data-field-error]").textContent =
        text;
      input.classList.toggle("is-invalid", text !== "");
      return text === "";
    }

    // Checks all sign-up fields and puts the cursor in the first wrong one
    function checkRegister(f) {
      var first = null;
      f.querySelectorAll("input[required]").forEach(function (input) {
        if (input.inputMode === "numeric")
          input.value = toEnglishDigits(input.value.trim());
        if (!checkField(input) && !first) first = input;
      });
      if (first) first.focus();
      return first === null;
    }

    function validate(f) {
      var fields = f.querySelectorAll("input[required]");
      for (var i = 0; i < fields.length; i++) {
        var input = fields[i];
        var label = input.closest("label").querySelector("span").textContent;
        input.value = input.value.trim();
        if (input.inputMode === "numeric")
          input.value = toEnglishDigits(input.value);

        if (!input.value)
          return { input: input, text: "وارد کردن " + label + " الزامی است" };
        if (input.maxLength === 11 && !/^09\d{9}$/.test(input.value))
          return {
            input: input,
            text: label + " باید ۱۱ رقم و با ۰۹ شروع شود",
          };
        if (input.type === "email" && !/^\S+@\S+\.\S+$/.test(input.value))
          return { input: input, text: label + " درست نیست" };
        if (input.name === "FullName" && input.value.indexOf(" ") < 0)
          return {
            input: input,
            text: "لطفا نام و نام خانوادگی را با فاصله وارد کنید",
          };
      }
      return null;
    }

    async function send(f) {
      if (f.dataset.authForm === "register") {
        // Sign-up: each field shows its own error
        if (!checkRegister(f)) return;
      } else {
        // Login forms: the old check, one error at the bottom
        var problem = validate(f);
        if (problem) {
          setError(f, problem.text);
          problem.input.focus();
          return;
        }
      }
      setError(f, "");

      var button = f.querySelector("[type=submit]");
      button.disabled = true;
      button.classList.add("is-loading");
      try {
        var response = await fetch(f.action, {
          method: "POST",
          body: new FormData(f),
          headers: {
            "X-Requested-With": "XMLHttpRequest",
            Accept: "application/json",
          },
          credentials: "same-origin",
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
        // The server can also send one error per field: { errors: { Mobile: "..." } }
        if (data.errors) {
          Object.keys(data.errors).forEach(function (name) {
            var input = f.querySelector('[name="' + name + '"]');
            if (!input) return;
            input
              .closest("label")
              .querySelector("[data-field-error]").textContent =
              data.errors[name];
            input.classList.add("is-invalid");
          });
        }
        return;
      }

      switch (data.step) {
        case "code":
          root.querySelector("[data-auth-mobile]").textContent =
            data.mobileNumber;
          form("code").reset();
          setError(form("code"), "");
          showLoginForm("code");
          break;
        case "registered":
          // Account is ready: open the mobile login with the number filled in
          form("register").reset();
          showPanel("login");
          history.replaceState(null, "", location.pathname);
          form("mobile").querySelector("input[name=MobileNumber]").value =
            data.mobileNumber || "";
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
        history.pushState(
          null,
          "",
          show.dataset.authShow === "register"
            ? "?mode=register"
            : location.pathname,
        );
        var first = root.querySelector(
          '[data-auth-panel="' +
            show.dataset.authShow +
            '"] form:not([hidden]) input:not([type=hidden])',
        );
        if (first) first.focus();
      }
    });
    // Sign-up: check a field when the user leaves it, and again while they fix it
    var registerForm = form("register");
    registerForm.addEventListener("focusout", function (e) {
      if (e.target.matches("input[required]")) checkField(e.target);
    });
    registerForm.addEventListener("input", function (e) {
      // Mobile: keep only digits (Persian digits become 0-9), so letters can't be typed
      if (e.target.name === "Mobile") {
        e.target.value = toEnglishDigits(e.target.value).replace(/\D/g, "");
      }
      if (e.target.classList.contains("is-invalid")) checkField(e.target);
      // A new password must be checked against the repeat password again
      if (e.target.name === "Password") {
        var confirm = registerForm.querySelector("[name=ConfirmPassword]");
        if (confirm.value) checkField(confirm);
      }
    });

    // The address shows which box is open (/Home/SignIn or /Home/SignIn?mode=register),
    // so the browser back button and a shared link open the same box
    window.addEventListener("popstate", function () {
      showPanel(
        new URLSearchParams(location.search).get("mode") === "register"
          ? "register"
          : "login",
      );
    });
  }

  document.querySelectorAll("[data-auth]").forEach(initAuth);
})();
