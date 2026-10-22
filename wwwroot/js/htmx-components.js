"use strict";
(() => {
  // tools/runtime/src/htmx-events.ts
  function getHtmxDetail(event) {
    return event instanceof CustomEvent && typeof event.detail === "object" && event.detail !== null ? event.detail : {};
  }

  // tools/runtime/src/failures.ts
  var container = window;
  function setFailureReporter(value) {
    container.__htmxComponentsFailureReporter = value;
  }
  function reportFailure(detail, title, message) {
    if (container.__htmxComponentsFailureReporter) {
      container.__htmxComponentsFailureReporter(detail, title, message);
    } else {
      console.warn(`${title}: ${message}`);
    }
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
        eventClass: getReplayEventClass(triggeringEvent),
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
        const loginOrigin = new URL(loginUrl, window.location.href).origin;
        const popup = window.open(loginUrl, "authPopup", "width=600,height=700");
        if (!popup) {
          reportFailure(detail, "Sign in required", "The sign-in popup was blocked. Allow popups for this site and try again.");
          return;
        }
        const loginResult = await new Promise(function(resolve) {
          let completed = false;
          const timeoutMs = 3e4;
          let closeTimer = 0;
          let timeoutTimer = 0;
          function finish(result) {
            if (completed) {
              return;
            }
            completed = true;
            window.removeEventListener("message", listener);
            window.clearInterval(closeTimer);
            window.clearTimeout(timeoutTimer);
            resolve(result);
          }
          function listener(messageEvent) {
            if (messageEvent.data === "login-success" && messageEvent.origin === loginOrigin && messageEvent.source === popup) {
              finish("success");
            }
          }
          closeTimer = window.setInterval(function() {
            if (popup.closed) {
              finish("closed");
            }
          }, 250);
          timeoutTimer = window.setTimeout(function() {
            finish("timeout");
          }, timeoutMs);
          window.addEventListener("message", listener);
        });
        if (loginResult !== "success") {
          reportFailure(detail, "Sign in required", loginResult === "closed" ? "The sign-in popup was closed. Please try again." : "Sign-in timed out. Please try again.");
          return;
        }
        if (requestContext?.elt instanceof Element && requestContext.elt.isConnected) {
          const { elt, eventType, eventClass, eventInit } = requestContext;
          const createEvent = getReplayEventFactory(eventClass);
          elt.dispatchEvent(createEvent(eventType, eventInit));
        }
      } catch {
        reportFailure(detail, "Sign in required", "Sign-in could not be completed. Please try again.");
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
  function getReplayEventClass(event) {
    if (event instanceof MouseEvent) {
      return "MouseEvent";
    }
    if (event instanceof KeyboardEvent) {
      return "KeyboardEvent";
    }
    if (event instanceof CustomEvent) {
      return "CustomEvent";
    }
    return "Event";
  }
  function getReplayEventFactory(eventClass) {
    switch (eventClass) {
      case "MouseEvent":
        return (type, init2) => new MouseEvent(type, init2);
      case "KeyboardEvent":
        return (type, init2) => new KeyboardEvent(type, init2);
      case "CustomEvent":
        return (type, init2) => new CustomEvent(type, init2);
      default:
        return (type, init2) => new Event(type, init2);
    }
  }

  // tools/runtime/src/behaviors/blur-save-coordination.ts
  var pendingBlurRequests = /* @__PURE__ */ new Set();
  var deferredSaves = /* @__PURE__ */ new Map();
  function installBlurSaveCoordination() {
    document.addEventListener("htmx:beforeSend", function(event) {
      const detail = getHtmxDetail(event);
      if (detail.elt instanceof Element && detail.requestConfig && detail.xhr && isBlurRequest(detail.elt, detail.requestConfig)) {
        pendingBlurRequests.add(detail.xhr);
      }
    });
    document.addEventListener("htmx:beforeRequest", function(event) {
      if (event.defaultPrevented) {
        return;
      }
      const detail = getHtmxDetail(event);
      const element = detail.elt;
      const config = detail.requestConfig;
      if (!(element instanceof Element) || !config || isBlurRequest(element, config) || !isSaveRequest(config)) {
        return;
      }
      const focusedInput = document.querySelector("input:focus, select:focus, textarea:focus");
      if (!focusedInput && pendingBlurRequests.size === 0 && !deferredSaves.has(element)) {
        return;
      }
      event.preventDefault();
      if (deferredSaves.has(element)) {
        return;
      }
      if (focusedInput instanceof HTMLElement) {
        focusedInput.blur();
      }
      deferSave(element, config.triggeringEvent?.type, detail);
    });
    for (const name of ["htmx:afterRequest", "htmx:responseError", "htmx:sendError", "htmx:timeout", "htmx:sendAbort"]) {
      document.addEventListener(name, function(event) {
        const xhr = getHtmxDetail(event).xhr;
        if (xhr) {
          pendingBlurRequests.delete(xhr);
        }
      });
    }
  }
  function deferSave(element, eventType, detail) {
    const deadline = Date.now() + 1e3;
    function retry() {
      deferredSaves.delete(element);
      if (!element.isConnected) {
        return;
      }
      if (pendingBlurRequests.size === 0) {
        window.htmx?.trigger(element, element instanceof HTMLFormElement ? "submit" : eventType || "click");
        return;
      }
      if (Date.now() >= deadline) {
        reportFailure(detail, "Save not completed", "Field updates are still pending. Wait for them to finish, then save again.");
        return;
      }
      deferredSaves.set(element, window.setTimeout(retry, 25));
    }
    deferredSaves.set(element, window.setTimeout(retry, 25));
  }
  function actionName(config) {
    if (!config.path) {
      return "";
    }
    try {
      return new URL(config.path, window.location.href).pathname.split("/").filter(Boolean).pop() || "";
    } catch {
      return "";
    }
  }
  function isBlurRequest(element, config) {
    const trigger = element.getAttribute("hx-trigger") || element.getAttribute("data-hx-trigger") || "";
    return /(?:^|[,\s])blur(?:$|[,\s\[])/.test(trigger) && ["SetValue", "UpdateField", "ValueChanged"].includes(actionName(config));
  }
  function isSaveRequest(config) {
    return ["Save", "Submit", "Update", "Create"].includes(actionName(config));
  }

  // tools/runtime/src/behaviors/error-handling.ts
  function installErrorHandling() {
    setFailureReporter((detail, title, message) => showErrorFragment(detail, createErrorFragment(title, message)));
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
    const title = source.querySelector("[data-hc-error-title]");
    const message = source.querySelector("[data-hc-error-message]");
    if (!title?.textContent?.trim() || !message?.textContent?.trim()) {
      return null;
    }
    const safeFragment = createErrorFragment(title.textContent, message.textContent);
    for (const name of ["data-hc-error-kind", "data-hc-component-id"]) {
      const value = source.getAttribute(name);
      if (value !== null) {
        safeFragment.setAttribute(name, value);
      }
    }
    return safeFragment;
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
      const result = guardSelector(selector, () => source.closest(selector.substring("closest ".length)), null);
      return result ? [result] : [];
    }
    if (selector.startsWith("find ")) {
      const root2 = scope || source;
      return querySelectorAll(root2, selector.substring("find ".length));
    }
    const root = scope || document;
    return querySelectorAll(root, selector);
  }
  function querySelectorAll(root, selector) {
    return guardSelector(selector, () => Array.from(root.querySelectorAll(selector)), []);
  }
  function querySelector(root, selector) {
    return guardSelector(selector, () => root.querySelector(selector), null);
  }
  function guardSelector(selector, query, fallback) {
    try {
      return query();
    } catch (error) {
      if (!(error instanceof DOMException) || error.name !== "SyntaxError") {
        throw error;
      }
      console.warn(`Htmx.Components ignored invalid selector: ${selector}`, error);
      return fallback;
    }
  }
  function collectMatches(root, selector) {
    const closest = root instanceof Element ? root.closest(selector) : null;
    return Array.from(/* @__PURE__ */ new Set([...closest ? [closest] : [], ...root.querySelectorAll(selector)]));
  }

  // tools/runtime/src/behaviors/modal.ts
  var activeModal = null;
  function syncModals(root) {
    collectMatches(root, "[data-hc-modal]").forEach((modal) => prepareModal(asModal(modal)));
  }
  function installModalBehavior() {
    document.addEventListener("keydown", function(event) {
      if (event.key === "Escape" && activeModal && typeof activeModal.showModal !== "function") {
        event.preventDefault();
        closeModal(activeModal);
      }
    });
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
    const body = configuredTarget ? querySelector(document, configuredTarget) : modal.querySelector("[data-hc-modal-body]");
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
    const body = bodySelector ? querySelector(document, bodySelector) : modal.querySelector("[data-hc-modal-body]");
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
    try {
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
      queueMicrotask(() => {
        if (event.defaultPrevented) {
          restorePendingState(event);
        }
      });
    } catch (error) {
      releasePendingState(state);
      throw error;
    }
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
    releasePendingState(state);
  }
  function releasePendingState(state) {
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
      const elements = querySelectorAll(scope, disableSelector).filter(function(element) {
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

  // tools/runtime/src/behaviors/table-inline-editing.ts
  function installTableInlineEditing() {
    if (window.htmx) {
      window.htmx.defineExtension("tableinline", {
        isInlineSwap: function() {
          return true;
        }
      });
    }
    document.addEventListener("htmx-components:table-connected", function(event) {
      const table = event.target;
      if (table instanceof Element) {
        syncTableEditing(table);
      }
    });
    document.addEventListener("htmx:afterSettle", function(event) {
      const detail = getHtmxDetail(event);
      syncTables(detail.target instanceof Element ? detail.target : document);
    });
  }
  function syncTables(root) {
    collectMatches(root, "[data-hc-table-component]").forEach(syncTableEditing);
  }
  function syncTableEditing(table) {
    const component = table.closest("[data-hc-table-component]") || table;
    const toggle = component.querySelector("[data-hc-table-edit-toggle]");
    if (!toggle) {
      return;
    }
    component.classList.toggle("editing-mode", toggle.classList.contains("editing-mode"));
  }

  // tools/runtime/src/behaviors/registry.ts
  var runtimeBehaviors = [
    {
      name: "page-state-headers",
      install: installPageStateHeaders
    },
    {
      name: "table-inline-editing",
      install: installTableInlineEditing,
      sync: syncTables
    },
    {
      name: "blur-save-coordination",
      install: installBlurSaveCoordination
    },
    {
      name: "request-lifecycle",
      install: installRequestLifecycleUx
    },
    {
      name: "error-handling",
      install: installErrorHandling
    },
    {
      name: "authentication-retry",
      install: installAuthenticationRetry
    },
    {
      name: "modal",
      install: installModalBehavior,
      sync: syncModals
    }
  ];
  var defaultBehaviors = runtimeBehaviors.map((behavior) => behavior.name);
  function findBehavior(name) {
    return runtimeBehaviors.find((behavior) => behavior.name === name);
  }

  // tools/runtime/src/config.ts
  var configElementId = "htmx-components-runtime-config";
  function getRuntimeConfig() {
    const element = document.getElementById(configElementId);
    if (!element) {
      return { behaviors: defaultBehaviors };
    }
    try {
      const config = JSON.parse(element.textContent || "{}");
      return {
        behaviors: Array.isArray(config.behaviors) ? normalizeBehaviors(config.behaviors) : defaultBehaviors
      };
    } catch (error) {
      console.warn("Htmx.Components runtime config could not be parsed.", error);
      return { behaviors: defaultBehaviors };
    }
  }
  function normalizeBehaviors(behaviors) {
    return behaviors.filter((behavior) => typeof behavior === "string" && defaultBehaviors.includes(behavior));
  }

  // tools/runtime/src/events.ts
  function dispatchComponentEvent(name, target, detail) {
    const eventTarget = target instanceof Element || target instanceof Document ? target : document;
    eventTarget.dispatchEvent(new CustomEvent(name, {
      bubbles: true,
      detail
    }));
  }

  // tools/runtime/src/custom-elements.ts
  var HtmxTableElement = class extends HTMLElement {
    connectedCallback() {
      dispatchComponentEvent("htmx-components:table-connected", this, { table: this });
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

  // tools/runtime/src/index.ts
  var runtimeState = getRuntimeState();
  for (const name of getRuntimeConfig().behaviors) {
    runtimeState.requestedBehaviors.add(name);
  }
  var activeRuntimeConfig = { behaviors: Array.from(runtimeState.installedBehaviors) };
  function init(root) {
    const initRoot = root instanceof Element || root instanceof Document ? root : document;
    for (const name of runtimeState.installedBehaviors) {
      findBehavior(name)?.sync?.(initRoot);
    }
    dispatchComponentEvent("htmx-components:load", initRoot, { root: initRoot });
  }
  window.HtmxComponents = {
    config: activeRuntimeConfig,
    init,
    installedBehaviors: activeRuntimeConfig.behaviors
  };
  if (!runtimeState.loadHandlerRegistered) {
    runtimeState.loadHandlerRegistered = true;
    document.addEventListener("htmx:load", function(event) {
      const root = getHtmxDetail(event).elt;
      if (!runtimeState.initialBody || !(root instanceof Element) || root === runtimeState.initialBody) {
        return;
      }
      window.HtmxComponents?.init(root);
    });
  }
  if (document.readyState === "loading") {
    if (!runtimeState.startupScheduled) {
      runtimeState.startupScheduled = true;
      document.addEventListener("DOMContentLoaded", start, { once: true });
    }
  } else {
    start();
  }
  function start() {
    if (!window.htmx && !runtimeState.warnedMissingHtmx) {
      runtimeState.warnedMissingHtmx = true;
      console.warn("Htmx.Components requires HTMX to load before the runtime. Load HTMX first, then the deferred runtime script.");
    }
    for (const name of runtimeState.requestedBehaviors) {
      if (!runtimeState.installedBehaviors.has(name)) {
        findBehavior(name)?.install();
        runtimeState.installedBehaviors.add(name);
      }
    }
    const behaviors = window.HtmxComponents?.config.behaviors;
    if (behaviors) {
      behaviors.splice(0, behaviors.length, ...runtimeState.installedBehaviors);
    }
    defineCustomElements();
    if (!runtimeState.initialBody) {
      runtimeState.initialBody = document.body;
      window.HtmxComponents?.init(document.body);
    }
  }
  function getRuntimeState() {
    const container2 = window;
    return container2.__htmxComponentsRuntimeState ?? (container2.__htmxComponentsRuntimeState = {
      installedBehaviors: /* @__PURE__ */ new Set(),
      requestedBehaviors: /* @__PURE__ */ new Set(),
      loadHandlerRegistered: false,
      startupScheduled: false,
      warnedMissingHtmx: false
    });
  }
})();
