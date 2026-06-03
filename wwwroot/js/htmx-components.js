(function () {
  "use strict";

  const configElementId = "htmx-components-config";
  const defaultScripts = [
    "page-state-headers",
    "table-inline-editing",
    "blur-save-coordination",
    "authentication-retry",
  ];
  const pendingBlurRequests = new Set();
  let authRetryRequestContext = null;
  let authRetryInProgress = false;

  function getRuntimeConfig() {
    const element = document.getElementById(configElementId);
    if (!element) {
      return { scripts: defaultScripts };
    }

    try {
      const config = JSON.parse(element.textContent || "{}");
      return {
        scripts: Array.isArray(config.Scripts)
          ? config.Scripts
          : Array.isArray(config.scripts)
            ? config.scripts
            : defaultScripts,
      };
    } catch (error) {
      console.warn("Htmx.Components runtime config could not be parsed.", error);
      return { scripts: defaultScripts };
    }
  }

  const runtimeConfig = getRuntimeConfig();

  function scriptEnabled(name) {
    return runtimeConfig.scripts.includes(name);
  }

  function defineElement(name, type) {
    if (!window.customElements || customElements.get(name)) {
      return;
    }

    customElements.define(name, type);
  }

  class HtmxTableElement extends HTMLElement {
    connectedCallback() {
      syncTableEditing(this);
      dispatchComponentEvent("htmx-components:table-connected", this, { table: this });
    }
  }

  class HtmxRequestScopeElement extends HTMLElement {
    connectedCallback() {
      this.dataset.hcRequestScope = this.dataset.hcRequestScope || "";
      dispatchComponentEvent("htmx-components:request-scope-connected", this, { scope: this });
    }
  }

  class HtmxErrorRegionElement extends HTMLElement {
    connectedCallback() {
      if (!this.hasAttribute("role")) {
        this.setAttribute("role", "status");
      }

      if (!this.hasAttribute("aria-live")) {
        this.setAttribute("aria-live", "polite");
      }
    }

    clear() {
      this.hidden = true;
      this.textContent = "";
    }

    show(message) {
      this.hidden = false;
      this.textContent = message;
    }
  }

  function defineCustomElements() {
    defineElement("htmx-table", HtmxTableElement);
    defineElement("htmx-request-scope", HtmxRequestScopeElement);
    defineElement("htmx-error-region", HtmxErrorRegionElement);
  }

  function init(root) {
    const initRoot = root instanceof Element || root instanceof Document ? root : document;

    syncTables(initRoot);
    dispatchComponentEvent("htmx-components:load", initRoot, { root: initRoot });
  }

  function syncTables(root) {
    const tables = root.matches?.("[data-hc-table-component]")
      ? [root]
      : Array.from(root.querySelectorAll?.("[data-hc-table-component]") || []);

    tables.forEach(syncTableEditing);
  }

  function syncTableEditing(table) {
    const component = table.closest?.("[data-hc-table-component]") || table;
    const toggle = component.querySelector("[data-hc-table-edit-toggle]");

    if (!toggle) {
      return;
    }

    component.classList.toggle("editing-mode", toggle.classList.contains("editing-mode"));
  }

  function dispatchComponentEvent(name, target, detail) {
    const eventTarget = target instanceof Element || target instanceof Document ? target : document;
    eventTarget.dispatchEvent(new CustomEvent(name, {
      bubbles: true,
      detail,
    }));
  }

  function installPageStateHeaders() {
    document.addEventListener("htmx:configRequest", function (event) {
      const pageStateInput = document.querySelector('input[name="page_state"]');

      if (!pageStateInput?.value) {
        return;
      }

      event.detail.headers["X-Page-State"] = pageStateInput.value;
    });
  }

  function installTableInlineEditing() {
    if (window.htmx) {
      htmx.defineExtension("tableinline", {
        isInlineSwap: function () {
          return true;
        },
      });
    }

    document.addEventListener("htmx:afterSettle", function (event) {
      syncTables(event.detail?.target || document);
    });
  }

  function installBlurSaveCoordination() {
    document.addEventListener("htmx:beforeRequest", function (event) {
      const element = event.detail.elt;
      const requestConfig = event.detail.requestConfig;

      if (!element || !requestConfig) {
        return;
      }

      if (isBlurRequest(element, requestConfig)) {
        pendingBlurRequests.add(element);
      }

      if (!isSaveRequest(requestConfig)) {
        return;
      }

      const focusedInput = document.querySelector("input:focus, select:focus, textarea:focus");
      const hasPendingBlur = pendingBlurRequests.size > 0;

      if (!focusedInput && !hasPendingBlur) {
        return;
      }

      event.preventDefault();

      if (focusedInput) {
        focusedInput.blur();
      }

      retryAfterBlur(element);
    });

    document.addEventListener("htmx:afterRequest", cleanupBlurRequest);
    document.addEventListener("htmx:responseError", cleanupBlurRequest);
  }

  function retryAfterBlur(element) {
    const maxRetries = 40;
    let retryCount = 0;

    const retry = function () {
      if (pendingBlurRequests.size === 0) {
        htmx.trigger(element, "click");
        return;
      }

      if (retryCount < maxRetries) {
        retryCount += 1;
        window.setTimeout(retry, 25);
        return;
      }

      console.warn("Blur-Save coordination timed out waiting for blur requests to complete.");
      htmx.trigger(element, "click");
    };

    window.setTimeout(retry, 25);
  }

  function cleanupBlurRequest(event) {
    const element = event.detail.elt;
    const requestConfig = event.detail.requestConfig;

    if (element && requestConfig && isBlurRequest(element, requestConfig)) {
      pendingBlurRequests.delete(element);
    }
  }

  function isBlurRequest(element, requestConfig) {
    return element.hasAttribute("hx-trigger")
      && element.getAttribute("hx-trigger").includes("blur")
      && requestConfig.path
      && (
        requestConfig.path.includes("/SetValue")
        || requestConfig.path.includes("/UpdateField")
        || requestConfig.path.includes("/ValueChanged")
      );
  }

  function isSaveRequest(requestConfig) {
    return requestConfig.path
      && (
        requestConfig.path.includes("/Save")
        || requestConfig.path.includes("/Submit")
        || requestConfig.path.includes("/Update")
        || requestConfig.path.includes("/Create")
      );
  }

  function installAuthenticationRetry() {
    document.body.addEventListener("htmx:beforeRequest", function (event) {
      const config = event.detail.requestConfig;
      const triggeringEvent = config?.triggeringEvent;

      if (!(triggeringEvent instanceof Event)) {
        return;
      }

      authRetryRequestContext = {
        elt: config.elt,
        eventType: triggeringEvent.type,
        eventClass: triggeringEvent.constructor.name,
        eventInit: getEventInit(triggeringEvent),
      };
    });

    document.body.addEventListener("htmx:responseError", async function (event) {
      const xhr = event.detail.xhr;
      const failureHeader = xhr.getResponseHeader("X-Auth-Failure");

      if (authRetryInProgress || xhr.status !== 401 || !failureHeader?.startsWith("popup-login:")) {
        return;
      }

      authRetryInProgress = true;

      try {
        const loginUrl = failureHeader.substring("popup-login:".length);
        window.open(loginUrl, "authPopup", "width=600,height=700");

        const loginSuccess = await new Promise(function (resolve) {
          window.addEventListener("message", function listener(messageEvent) {
            if (messageEvent.data === "login-success") {
              window.removeEventListener("message", listener);
              resolve(true);
            }
          });
        });

        if (loginSuccess && authRetryRequestContext?.elt) {
          const { elt, eventType, eventClass, eventInit } = authRetryRequestContext;
          const EventCtor = window[eventClass] || Event;
          elt.dispatchEvent(new EventCtor(eventType, eventInit));
        }
      } finally {
        authRetryInProgress = false;
        authRetryRequestContext = null;
      }
    });
  }

  function getEventInit(event) {
    const base = {
      bubbles: event.bubbles,
      cancelable: event.cancelable,
      composed: event.composed,
    };

    if (event instanceof MouseEvent) {
      return {
        ...base,
        screenX: event.screenX,
        screenY: event.screenY,
        clientX: event.clientX,
        clientY: event.clientY,
        ctrlKey: event.ctrlKey,
        shiftKey: event.shiftKey,
        altKey: event.altKey,
        metaKey: event.metaKey,
        button: event.button,
        buttons: event.buttons,
        relatedTarget: event.relatedTarget,
      };
    }

    if (event instanceof KeyboardEvent) {
      return {
        ...base,
        key: event.key,
        code: event.code,
        location: event.location,
        ctrlKey: event.ctrlKey,
        shiftKey: event.shiftKey,
        altKey: event.altKey,
        metaKey: event.metaKey,
        repeat: event.repeat,
        isComposing: event.isComposing,
      };
    }

    if (event instanceof CustomEvent) {
      return {
        ...base,
        detail: event.detail,
      };
    }

    return base;
  }

  defineCustomElements();

  if (scriptEnabled("page-state-headers")) {
    installPageStateHeaders();
  }

  if (scriptEnabled("table-inline-editing")) {
    installTableInlineEditing();
  }

  if (scriptEnabled("blur-save-coordination")) {
    installBlurSaveCoordination();
  }

  if (scriptEnabled("authentication-retry")) {
    installAuthenticationRetry();
  }

  window.HtmxComponents = {
    config: runtimeConfig,
    init,
  };

  if (window.htmx?.onLoad) {
    htmx.onLoad(init);
  } else if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () {
      init(document.body);
    }, { once: true });
  } else {
    init(document.body);
  }
})();
