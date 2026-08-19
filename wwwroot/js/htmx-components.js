"use strict";
(() => {
  // tools/runtime/src/config.ts
  var configElementId = "htmx-components-config";
  var defaultScripts = [
    "page-state-headers",
    "table-inline-editing",
    "blur-save-coordination",
    "request-lifecycle",
    "error-handling",
    "authentication-retry",
    "modal"
  ];
  function getRuntimeConfig() {
    const element = document.getElementById(configElementId);
    if (!element) {
      return { scripts: defaultScripts };
    }
    try {
      const config = JSON.parse(element.textContent || "{}");
      return {
        scripts: Array.isArray(config.Scripts) ? config.Scripts : Array.isArray(config.scripts) ? config.scripts : defaultScripts
      };
    } catch (error) {
      console.warn("Htmx.Components runtime config could not be parsed.", error);
      return { scripts: defaultScripts };
    }
  }
  function scriptEnabled(runtimeConfig2, name) {
    return runtimeConfig2.scripts.includes(name);
  }

  // tools/runtime/src/events.ts
  function dispatchComponentEvent(name, target, detail) {
    const eventTarget = target instanceof Element || target instanceof Document ? target : document;
    eventTarget.dispatchEvent(new CustomEvent(name, {
      bubbles: true,
      detail
    }));
  }

  // tools/runtime/src/htmx-events.ts
  function getHtmxDetail(event) {
    return event instanceof CustomEvent && typeof event.detail === "object" && event.detail !== null ? event.detail : {};
  }

  // tools/runtime/src/behaviors/table-inline-editing.ts
  function installTableInlineEditing() {
    if (window.htmx) {
      window.htmx.defineExtension("tableinline", {
        isInlineSwap: function() {
          return true;
        }
      });
    }
    document.addEventListener("htmx:afterSettle", function(event) {
      const detail = getHtmxDetail(event);
      syncTables(detail.target instanceof Element ? detail.target : document);
    });
  }
  function syncTables(root) {
    const tables = [];
    const closestTable = root instanceof Element ? root.closest("[data-hc-table-component]") : null;
    if (closestTable) {
      tables.push(closestTable);
    }
    if (root instanceof Element && root.matches("[data-hc-table-component]") && root !== closestTable) {
      tables.push(root);
    }
    tables.push(...Array.from(root.querySelectorAll("[data-hc-table-component]")).filter((table) => !tables.includes(table)));
    tables.forEach(syncTableEditing);
  }
  function syncTableEditing(table) {
    const component = table.closest("[data-hc-table-component]") || table;
    const toggle = component.querySelector("[data-hc-table-edit-toggle]");
    if (!toggle) {
      return;
    }
    component.classList.toggle("editing-mode", toggle.classList.contains("editing-mode"));
  }
  function dispatchTableConnected(table) {
    syncTableEditing(table);
    dispatchComponentEvent("htmx-components:table-connected", table, { table });
  }

  // tools/runtime/src/custom-elements.ts
  var HtmxTableElement = class extends HTMLElement {
    connectedCallback() {
      dispatchTableConnected(this);
    }
  };
  var HtmxRequestScopeElement = class extends HTMLElement {
    connectedCallback() {
      this.dataset.hcRequestScope = this.dataset.hcRequestScope || "";
      dispatchComponentEvent("htmx-components:request-scope-connected", this, { scope: this });
    }
  };
  var HtmxErrorRegionElement = class extends HTMLElement {
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
      this.replaceChildren();
    }
    show(message) {
      this.hidden = false;
      this.textContent = message;
    }
    showFragment(fragment) {
      this.hidden = false;
      this.replaceChildren(fragment);
    }
  };
  function defineElement(name, type) {
    if (!window.customElements || customElements.get(name)) {
      return;
    }
    customElements.define(name, type);
  }
  function defineCustomElements() {
    defineElement("htmx-table", HtmxTableElement);
    defineElement("htmx-request-scope", HtmxRequestScopeElement);
    defineElement("htmx-error-region", HtmxErrorRegionElement);
  }

  // tools/runtime/src/behaviors/authentication-retry.ts
  var authRetryRequestContexts = /* @__PURE__ */ new WeakMap();
  var authRetryInProgress = false;
  function installAuthenticationRetry() {
    document.body.addEventListener("htmx:beforeRequest", function(event) {
      const detail = getHtmxDetail(event);
      const config = detail.requestConfig;
      const triggeringEvent = config?.triggeringEvent;
      if (!(triggeringEvent instanceof Event)) {
        return;
      }
      const xhr = detail.xhr;
      if (!xhr) {
        return;
      }
      authRetryRequestContexts.set(xhr, {
        elt: detail.elt,
        eventType: triggeringEvent.type,
        eventClass: triggeringEvent.constructor.name,
        eventInit: getEventInit(triggeringEvent)
      });
    });
    document.body.addEventListener("htmx:responseError", async function(event) {
      const detail = getHtmxDetail(event);
      const xhr = detail.xhr;
      if (!xhr) {
        return;
      }
      const failureHeader = typeof xhr.getResponseHeader === "function" ? xhr.getResponseHeader("X-Auth-Failure") : null;
      const requestContext = authRetryRequestContexts.get(xhr);
      authRetryRequestContexts.delete(xhr);
      if (authRetryInProgress || xhr.status !== 401 || !failureHeader?.startsWith("popup-login:")) {
        return;
      }
      authRetryInProgress = true;
      try {
        const loginUrl = failureHeader.substring("popup-login:".length);
        const popup = window.open(loginUrl, "authPopup", "width=600,height=700");
        const loginSuccess = await new Promise(function(resolve) {
          let completed = false;
          const timeoutMs = 3e4;
          let closeTimer = 0;
          let timeoutTimer = 0;
          function finish(success) {
            if (completed) {
              return;
            }
            completed = true;
            window.removeEventListener("message", listener);
            window.clearInterval(closeTimer);
            window.clearTimeout(timeoutTimer);
            resolve(success);
          }
          function listener(messageEvent) {
            if (messageEvent.data === "login-success") {
              finish(true);
            }
          }
          closeTimer = window.setInterval(function() {
            if (popup?.closed) {
              finish(false);
            }
          }, 250);
          timeoutTimer = window.setTimeout(function() {
            finish(false);
          }, timeoutMs);
          window.addEventListener("message", listener);
        });
        if (loginSuccess && requestContext?.elt instanceof Element && requestContext.elt.isConnected) {
          const { elt, eventType, eventClass, eventInit } = requestContext;
          const EventCtor = getEventConstructor(eventClass);
          elt.dispatchEvent(new EventCtor(eventType, eventInit));
        }
      } finally {
        authRetryInProgress = false;
      }
    });
    document.body.addEventListener("htmx:afterRequest", function(event) {
      const xhr = getHtmxDetail(event).xhr;
      if (xhr) {
        authRetryRequestContexts.delete(xhr);
      }
    });
  }
  function getEventInit(event) {
    const base = {
      bubbles: event.bubbles,
      cancelable: event.cancelable,
      composed: event.composed
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
        relatedTarget: event.relatedTarget
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
        isComposing: event.isComposing
      };
    }
    if (event instanceof CustomEvent) {
      return {
        ...base,
        detail: event.detail
      };
    }
    return base;
  }
  function getEventConstructor(eventClass) {
    const constructors = window;
    return constructors[eventClass] || Event;
  }

  // tools/runtime/src/behaviors/blur-save-coordination.ts
  var pendingBlurRequests = /* @__PURE__ */ new Set();
  function installBlurSaveCoordination() {
    document.addEventListener("htmx:beforeRequest", function(event) {
      const detail = getHtmxDetail(event);
      const element = detail.elt;
      const requestConfig = detail.requestConfig;
      if (!(element instanceof Element) || !requestConfig) {
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
      if (focusedInput instanceof HTMLElement) {
        focusedInput.blur();
      }
      retryAfterBlur({
        element,
        eventType: requestConfig.triggeringEvent?.type
      });
    });
    document.addEventListener("htmx:afterRequest", cleanupBlurRequest);
    document.addEventListener("htmx:responseError", cleanupBlurRequest);
  }
  function retryAfterBlur(request) {
    const maxRetries = 40;
    let retryCount = 0;
    const element = request.element;
    const eventType = request.eventType || (element instanceof HTMLFormElement ? "submit" : "click");
    const retry = function() {
      if (pendingBlurRequests.size === 0) {
        replayDeferredRequest(element, eventType);
        return;
      }
      if (retryCount < maxRetries) {
        retryCount += 1;
        window.setTimeout(retry, 25);
        return;
      }
      console.warn("Blur-Save coordination timed out waiting for blur requests to complete.");
      replayDeferredRequest(element, eventType);
    };
    window.setTimeout(retry, 25);
  }
  function replayDeferredRequest(element, eventType) {
    if (!element.isConnected) {
      return;
    }
    window.htmx?.trigger(element, element instanceof HTMLFormElement ? "submit" : eventType);
  }
  function cleanupBlurRequest(event) {
    const detail = getHtmxDetail(event);
    const element = detail.elt;
    const requestConfig = detail.requestConfig;
    if (element instanceof Element && requestConfig && isBlurRequest(element, requestConfig)) {
      pendingBlurRequests.delete(element);
    }
  }
  function isBlurRequest(element, requestConfig) {
    return element.hasAttribute("hx-trigger") && (element.getAttribute("hx-trigger") || "").includes("blur") && !!requestConfig.path && (requestConfig.path.includes("/SetValue") || requestConfig.path.includes("/UpdateField") || requestConfig.path.includes("/ValueChanged"));
  }
  function isSaveRequest(requestConfig) {
    return !!requestConfig.path && (requestConfig.path.includes("/Save") || requestConfig.path.includes("/Submit") || requestConfig.path.includes("/Update") || requestConfig.path.includes("/Create"));
  }

  // tools/runtime/src/behaviors/error-handling.ts
  function installErrorHandling() {
    ensureGlobalErrorRegion();
    document.addEventListener("htmx:beforeRequest", clearRequestError);
    document.addEventListener("htmx:responseError", showResponseError);
    document.addEventListener("htmx:sendError", function(event) {
      showClientError(event, "Request failed", "The request could not be sent. Check your connection and try again.");
    });
    document.addEventListener("htmx:timeout", function(event) {
      showClientError(event, "Request timed out", "The request took too long. Please try again.");
    });
    document.addEventListener("htmx:swapError", function(event) {
      showClientError(event, "Display failed", "The response could not be displayed. Please try again.");
    });
  }
  function clearRequestError(event) {
    const region = findErrorRegion(getHtmxDetail(event));
    if (region) {
      clearErrorRegion(region);
    }
  }
  function showResponseError(event) {
    const detail = getHtmxDetail(event);
    const xhr = detail.xhr;
    const failureHeader = typeof xhr?.getResponseHeader === "function" ? xhr.getResponseHeader("X-Auth-Failure") : null;
    if (xhr?.status === 401 && failureHeader?.startsWith("popup-login:")) {
      return;
    }
    const parsed = parseErrorFragment(xhr?.responseText);
    if (parsed) {
      showErrorFragment(detail, parsed);
      return;
    }
    const status = xhr?.status || 0;
    const fallback = getStatusError(status);
    showClientError(event, fallback.title, fallback.message);
  }
  function showClientError(event, title, message) {
    showErrorFragment(getHtmxDetail(event), createErrorFragment(title, message));
  }
  function showErrorFragment(detail, fragment) {
    const region = findErrorRegion(detail) || ensureGlobalErrorRegion();
    if (typeof region.showFragment === "function") {
      region.showFragment(fragment);
      return;
    }
    region.hidden = false;
    region.replaceChildren(fragment);
  }
  function clearErrorRegion(region) {
    if (typeof region.clear === "function") {
      region.clear();
      return;
    }
    region.hidden = true;
    region.replaceChildren();
  }
  function parseErrorFragment(responseText) {
    if (!responseText) {
      return null;
    }
    const documentFragment = document.createElement("template");
    documentFragment.innerHTML = responseText.trim();
    const source = documentFragment.content.querySelector("[data-hc-error-fragment]");
    if (!source) {
      return null;
    }
    return source.cloneNode(true);
  }
  function createErrorFragment(title, message) {
    const wrapper = document.createElement("div");
    wrapper.dataset.hcErrorFragment = "";
    const titleElement = document.createElement("strong");
    titleElement.dataset.hcErrorTitle = "";
    titleElement.textContent = title;
    const messageElement = document.createElement("span");
    messageElement.dataset.hcErrorMessage = "";
    messageElement.textContent = message;
    wrapper.append(titleElement, " ", messageElement);
    return wrapper;
  }
  function getStatusError(status) {
    if (status === 400) {
      return {
        title: "Request not completed",
        message: "The request could not be completed. Check your entry and try again."
      };
    }
    if (status === 401) {
      return {
        title: "Sign in required",
        message: "Please sign in and try again."
      };
    }
    if (status === 403) {
      return {
        title: "Access denied",
        message: "You do not have permission to perform this action."
      };
    }
    if (status >= 500) {
      return {
        title: "Something went wrong",
        message: "The request could not be completed. Please try again."
      };
    }
    return {
      title: "Request failed",
      message: "The request could not be completed. Please try again."
    };
  }
  function findErrorRegion(detail) {
    const trigger = detail.elt instanceof Element ? detail.elt : null;
    const target = detail.target instanceof Element ? detail.target : null;
    const scope = trigger?.closest("htmx-request-scope, [data-hc-request-scope], [data-hc-table-component]") || target?.closest("htmx-request-scope, [data-hc-request-scope], [data-hc-table-component]") || null;
    return asErrorRegion(scope?.querySelector("htmx-error-region, [data-hc-error-region]") || document.querySelector("htmx-error-region[data-hc-global-error-region], [data-hc-global-error-region]"));
  }
  function ensureGlobalErrorRegion() {
    let region = document.querySelector("htmx-error-region[data-hc-global-error-region], [data-hc-global-error-region]");
    if (region) {
      return asErrorRegion(region);
    }
    region = document.createElement("htmx-error-region");
    region.setAttribute("data-hc-global-error-region", "");
    region.setAttribute("data-hc-error-region", "");
    if (region instanceof HTMLElement) {
      region.hidden = true;
    } else {
      region.setAttribute("hidden", "");
    }
    document.body.prepend(region);
    return asErrorRegion(region);
  }
  function asErrorRegion(region) {
    return region;
  }

  // tools/runtime/src/behaviors/modal.ts
  var activeModal = null;
  function syncModals(root) {
    const modals = [];
    const closestModal = root instanceof Element ? root.closest("[data-hc-modal]") : null;
    if (closestModal) {
      modals.push(closestModal);
    }
    if (root instanceof Element && root.matches("[data-hc-modal]") && root !== closestModal) {
      modals.push(root);
    }
    modals.push(...Array.from(root.querySelectorAll("[data-hc-modal]")).filter((modal) => !modals.includes(modal)));
    modals.forEach((modal) => prepareModal(asModal(modal)));
  }
  function installModalBehavior() {
    document.addEventListener("click", function(event) {
      const closeTrigger = event.target instanceof Element ? event.target.closest("[data-hc-modal-close]") : null;
      if (!closeTrigger) {
        return;
      }
      const modal = closeTrigger.closest("[data-hc-modal]");
      if (!modal) {
        return;
      }
      closeModal(asModal(modal));
    });
    document.addEventListener("htmx:afterSwap", function(event) {
      const detail = getHtmxDetail(event);
      const trigger = resolveHtmxRequestTrigger(detail);
      const target = detail.target;
      if (!(trigger instanceof Element) || !(target instanceof Element)) {
        return;
      }
      const modalId = trigger.getAttribute("data-hc-open-modal");
      if (!modalId) {
        return;
      }
      const modal = document.getElementById(modalId);
      if (!modal || !modal.matches("[data-hc-modal]") || !requestTargetedModalBody(trigger, modal, target)) {
        return;
      }
      openModal(asModal(modal), trigger);
    });
  }
  function prepareModal(modal) {
    if (modal.dataset.hcModalPrepared === "true") {
      return;
    }
    modal.dataset.hcModalPrepared = "true";
    modal.addEventListener("close", function() {
      handleModalClosed(modal);
    });
  }
  function resolveHtmxRequestTrigger(detail) {
    const requestElement = detail.requestConfig?.elt;
    if (requestElement instanceof Element) {
      return requestElement;
    }
    return detail.elt instanceof Element ? detail.elt : null;
  }
  function requestTargetedModalBody(trigger, modal, target) {
    const configuredTarget = modal.getAttribute("data-hc-modal-body-target");
    const targetSelector = trigger.getAttribute("hx-target") || trigger.getAttribute("data-hx-target");
    const body = configuredTarget ? document.querySelector(configuredTarget) : modal.querySelector("[data-hc-modal-body]");
    if (!body || target !== body) {
      return false;
    }
    return !targetSelector || targetSelector === configuredTarget || targetSelector === `#${body.id}`;
  }
  function openModal(modal, opener) {
    prepareModal(modal);
    if (activeModal && activeModal !== modal) {
      closeModal(activeModal, { restoreFocus: false });
    }
    modal.hcModalOpener = opener;
    activeModal = modal;
    if (typeof modal.showModal === "function" && !modal.open) {
      modal.showModal();
      return;
    }
    if (!modal.open) {
      modal.setAttribute("open", "");
    }
  }
  function closeModal(modal, options) {
    const settings = options || {};
    modal.hcModalRestoreFocus = settings.restoreFocus !== false;
    if (typeof modal.close === "function" && modal.open) {
      modal.close();
      return;
    }
    modal.removeAttribute("open");
    handleModalClosed(modal);
  }
  function handleModalClosed(modal) {
    const bodySelector = modal.getAttribute("data-hc-modal-body-target");
    const body = bodySelector ? document.querySelector(bodySelector) : modal.querySelector("[data-hc-modal-body]");
    if (body) {
      body.replaceChildren();
    }
    if (activeModal === modal) {
      activeModal = null;
    }
    const opener = modal.hcModalOpener;
    const shouldRestoreFocus = modal.hcModalRestoreFocus !== false;
    modal.hcModalOpener = null;
    modal.hcModalRestoreFocus = true;
    if (shouldRestoreFocus && opener instanceof HTMLElement && opener.isConnected) {
      opener.focus();
    }
  }
  function asModal(modal) {
    return modal;
  }

  // tools/runtime/src/behaviors/page-state-headers.ts
  function installPageStateHeaders() {
    document.addEventListener("htmx:configRequest", function(event) {
      const pageStateInput = document.querySelector('input[name="page_state"]');
      if (!pageStateInput?.value) {
        return;
      }
      const detail = getHtmxDetail(event);
      if (!detail.headers) {
        return;
      }
      detail.headers["X-Page-State"] = pageStateInput.value;
    });
  }

  // tools/runtime/src/selectors.ts
  function resolveElementSelector(source, selector, scope) {
    return resolveElementSelectorAll(source, selector, scope)[0] || null;
  }
  function resolveElementSelectorAll(source, selector, scope) {
    if (!selector) {
      return [];
    }
    if (selector === "this") {
      return [source];
    }
    if (selector.startsWith("closest ")) {
      const result = source.closest(selector.substring("closest ".length));
      return result ? [result] : [];
    }
    if (selector.startsWith("find ")) {
      const root2 = scope || source;
      return Array.from(root2.querySelectorAll(selector.substring("find ".length)));
    }
    const root = scope || document;
    return Array.from(root.querySelectorAll(selector));
  }

  // tools/runtime/src/behaviors/request-lifecycle.ts
  var pendingRequestStates = /* @__PURE__ */ new WeakMap();
  var pendingElementReferences = /* @__PURE__ */ new WeakMap();
  function installRequestLifecycleUx() {
    document.addEventListener("click", suppressPendingActivation, true);
    document.addEventListener("keydown", suppressPendingActivation, true);
    document.addEventListener("htmx:beforeRequest", startPendingState);
    document.addEventListener("htmx:afterRequest", restorePendingState);
    document.addEventListener("htmx:responseError", restorePendingState);
    document.addEventListener("htmx:sendError", restorePendingState);
    document.addEventListener("htmx:sendAbort", restorePendingState);
    document.addEventListener("htmx:timeout", restorePendingState);
  }
  function startPendingState(event) {
    if (event.defaultPrevented) {
      return;
    }
    const detail = getHtmxDetail(event);
    const trigger = detail.elt;
    const requestKey = getRequestKey(detail);
    if (!(trigger instanceof Element) || !requestKey) {
      return;
    }
    restorePendingState(event);
    const scope = resolveRequestScope(trigger, detail);
    const state = { releases: [] };
    if (scope) {
      retainPendingReference(state, scope, "scope", function() {
        const previousBusy = scope.getAttribute("aria-busy");
        scope.classList.add("hc-request-pending");
        scope.setAttribute("aria-busy", "true");
        return { previousBusy };
      }, function(previous) {
        scope.classList.remove("hc-request-pending");
        restoreNullableAttribute(scope, "aria-busy", previous.previousBusy);
      });
    }
    const disableMode = getPendingOption(trigger, scope, "pendingDisable", "trigger");
    disablePendingElements(state, trigger, scope, disableMode);
    dimStaleRegions(state, trigger, scope, detail);
    showPendingIndicators(state, trigger, scope);
    pendingRequestStates.set(requestKey, state);
  }
  function restorePendingState(event) {
    const requestKey = getRequestKey(getHtmxDetail(event));
    if (!requestKey) {
      return;
    }
    const state = pendingRequestStates.get(requestKey);
    if (!state) {
      return;
    }
    pendingRequestStates.delete(requestKey);
    for (let index = state.releases.length - 1; index >= 0; index -= 1) {
      state.releases[index]();
    }
  }
  function getRequestKey(detail) {
    return detail.xhr || detail.requestConfig || null;
  }
  function resolveRequestScope(trigger, detail) {
    const selector = trigger.getAttribute("data-hc-request-scope-selector");
    const configuredScope = selector ? resolveElementSelector(trigger, selector) : null;
    if (configuredScope) {
      return configuredScope;
    }
    return trigger.closest("htmx-request-scope, [data-hc-request-scope]") || (detail.target instanceof Element ? detail.target.closest("htmx-request-scope, [data-hc-request-scope]") : null);
  }
  function disablePendingElements(state, trigger, scope, disableMode) {
    if (disableMode === "none") {
      return;
    }
    if (disableMode === "scope" && scope) {
      const disableSelector = getPendingOption(trigger, scope, "pendingDisableSelector", null) || "button, input, select, textarea, a[href], [tabindex]";
      const elements = Array.from(scope.querySelectorAll(disableSelector)).filter(function(element) {
        return element !== scope && !element.hasAttribute("data-hc-no-pending-disable");
      });
      elements.forEach(function(element) {
        disableElementForPending(state, element);
      });
      return;
    }
    disableElementForPending(state, trigger);
  }
  function disableElementForPending(state, element) {
    retainPendingReference(state, element, "disable", function() {
      const previousDisabled = element.hasAttribute("disabled");
      const previousAriaDisabled = element.getAttribute("aria-disabled");
      const previousTabIndex = element.getAttribute("tabindex");
      if (isDisableableFormElement(element)) {
        element.disabled = true;
      } else {
        element.setAttribute("tabindex", "-1");
      }
      element.setAttribute("aria-disabled", "true");
      element.classList.add("hc-pending-disabled");
      return {
        previousDisabled,
        previousAriaDisabled,
        previousTabIndex
      };
    }, function(previous) {
      if (isDisableableFormElement(element)) {
        element.disabled = previous.previousDisabled;
      } else {
        restoreNullableAttribute(element, "tabindex", previous.previousTabIndex);
      }
      restoreNullableAttribute(element, "aria-disabled", previous.previousAriaDisabled);
      element.classList.remove("hc-pending-disabled");
    });
  }
  function isDisableableFormElement(element) {
    return element instanceof HTMLButtonElement || element instanceof HTMLInputElement || element instanceof HTMLSelectElement || element instanceof HTMLTextAreaElement || element instanceof HTMLOptionElement || element instanceof HTMLOptGroupElement || element instanceof HTMLFieldSetElement;
  }
  function suppressPendingActivation(event) {
    const target = event.target instanceof Element ? event.target.closest(".hc-pending-disabled") : null;
    if (!target) {
      return;
    }
    if (event instanceof KeyboardEvent && event.key !== "Enter" && event.key !== " ") {
      return;
    }
    event.preventDefault();
    event.stopImmediatePropagation();
  }
  function dimStaleRegions(state, trigger, scope, detail) {
    const selector = getPendingOption(trigger, scope, "staleRegionSelector", null);
    let regions = selector ? resolveElementSelectorAll(trigger, selector, scope) : scope ? Array.from(scope.querySelectorAll("[data-hc-stale-region]")) : [];
    if (regions.length === 0 && detail.target instanceof Element) {
      regions = [detail.target];
    }
    regions.forEach(function(region) {
      retainPendingReference(state, region, "stale", function() {
        const previousBusy = region.getAttribute("aria-busy");
        region.classList.add("hc-stale-region");
        region.setAttribute("aria-busy", "true");
        return { previousBusy };
      }, function(previous) {
        region.classList.remove("hc-stale-region");
        restoreNullableAttribute(region, "aria-busy", previous.previousBusy);
      });
    });
  }
  function showPendingIndicators(state, trigger, scope) {
    const selector = getPendingOption(trigger, scope, "requestIndicatorSelector", null);
    const indicators = selector ? resolveElementSelectorAll(trigger, selector, scope) : scope ? Array.from(scope.querySelectorAll("[data-hc-request-indicator]")) : [];
    indicators.forEach(function(indicator) {
      retainPendingReference(state, indicator, "indicator", function() {
        const previousHidden = indicator instanceof HTMLElement ? indicator.hidden : indicator.hasAttribute("hidden");
        const previousAriaHidden = indicator.getAttribute("aria-hidden");
        if (indicator instanceof HTMLElement) {
          indicator.hidden = false;
        } else {
          indicator.removeAttribute("hidden");
        }
        indicator.setAttribute("aria-hidden", "false");
        indicator.classList.add("hc-request-indicator-active");
        return {
          previousHidden,
          previousAriaHidden
        };
      }, function(previous) {
        if (indicator instanceof HTMLElement) {
          indicator.hidden = previous.previousHidden;
        } else if (previous.previousHidden) {
          indicator.setAttribute("hidden", "");
        } else {
          indicator.removeAttribute("hidden");
        }
        restoreNullableAttribute(indicator, "aria-hidden", previous.previousAriaHidden);
        indicator.classList.remove("hc-request-indicator-active");
      });
    });
  }
  function getPendingOption(trigger, scope, optionName, fallback) {
    const attribute = "data-hc-" + optionName.replace(/[A-Z]/g, function(letter) {
      return "-" + letter.toLowerCase();
    });
    if (trigger.hasAttribute(attribute)) {
      return trigger.getAttribute(attribute);
    }
    if (scope?.hasAttribute(attribute)) {
      return scope.getAttribute(attribute);
    }
    return fallback;
  }
  function retainPendingReference(state, element, key, apply, restore) {
    let references = pendingElementReferences.get(element);
    if (!references) {
      references = /* @__PURE__ */ new Map();
      pendingElementReferences.set(element, references);
    }
    let entry = references.get(key);
    if (!entry) {
      entry = {
        count: 0,
        previous: apply()
      };
      references.set(key, entry);
    }
    entry.count += 1;
    state.releases.push(function() {
      entry.count -= 1;
      if (entry.count > 0) {
        return;
      }
      references.delete(key);
      restore(entry.previous);
      if (references.size === 0) {
        pendingElementReferences.delete(element);
      }
    });
  }
  function restoreNullableAttribute(element, name, value) {
    if (value === null) {
      element.removeAttribute(name);
      return;
    }
    element.setAttribute(name, value);
  }

  // tools/runtime/src/index.ts
  var runtimeConfig = getRuntimeConfig();
  function init(root) {
    const initRoot = root instanceof Element || root instanceof Document ? root : document;
    syncTables(initRoot);
    syncModals(initRoot);
    dispatchComponentEvent("htmx-components:load", initRoot, { root: initRoot });
  }
  defineCustomElements();
  if (scriptEnabled(runtimeConfig, "page-state-headers")) {
    installPageStateHeaders();
  }
  if (scriptEnabled(runtimeConfig, "table-inline-editing")) {
    installTableInlineEditing();
  }
  if (scriptEnabled(runtimeConfig, "blur-save-coordination")) {
    installBlurSaveCoordination();
  }
  if (scriptEnabled(runtimeConfig, "request-lifecycle")) {
    installRequestLifecycleUx();
  }
  if (scriptEnabled(runtimeConfig, "error-handling")) {
    installErrorHandling();
  }
  if (scriptEnabled(runtimeConfig, "authentication-retry")) {
    installAuthenticationRetry();
  }
  if (scriptEnabled(runtimeConfig, "modal")) {
    installModalBehavior();
  }
  window.HtmxComponents = {
    config: runtimeConfig,
    init
  };
  if (window.htmx?.onLoad) {
    window.htmx.onLoad(init);
  } else if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function() {
      init(document.body);
    }, { once: true });
  } else {
    init(document.body);
  }
})();
