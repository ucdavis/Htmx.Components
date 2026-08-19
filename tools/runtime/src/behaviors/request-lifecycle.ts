import { getHtmxDetail, HtmxEventDetail } from "../htmx-events";
import { resolveElementSelector, resolveElementSelectorAll } from "../selectors";

interface PendingState {
  releases: Array<() => void>;
}

interface PendingReference<TPrevious> {
  count: number;
  previous: TPrevious;
}

const pendingRequestStates = new WeakMap<object, PendingState>();
const pendingElementReferences = new WeakMap<Element, Map<string, PendingReference<unknown>>>();

export function installRequestLifecycleUx(): void {
  document.addEventListener("click", suppressPendingActivation, true);
  document.addEventListener("keydown", suppressPendingActivation, true);
  document.addEventListener("htmx:beforeRequest", startPendingState);
  document.addEventListener("htmx:afterRequest", restorePendingState);
  document.addEventListener("htmx:responseError", restorePendingState);
  document.addEventListener("htmx:sendError", restorePendingState);
  document.addEventListener("htmx:sendAbort", restorePendingState);
  document.addEventListener("htmx:timeout", restorePendingState);
}

function startPendingState(event: Event): void {
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
  const state: PendingState = { releases: [] };

  if (scope) {
    retainPendingReference(state, scope, "scope", function () {
      const previousBusy = scope.getAttribute("aria-busy");
      scope.classList.add("hc-request-pending");
      scope.setAttribute("aria-busy", "true");
      return { previousBusy };
    }, function (previous) {
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

function restorePendingState(event: Event): void {
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

function getRequestKey(detail: HtmxEventDetail): object | null {
  return detail.xhr || detail.requestConfig || null;
}

function resolveRequestScope(trigger: Element, detail: HtmxEventDetail): Element | null {
  const selector = trigger.getAttribute("data-hc-request-scope-selector");
  const configuredScope = selector ? resolveElementSelector(trigger, selector) : null;

  if (configuredScope) {
    return configuredScope;
  }

  return trigger.closest("htmx-request-scope, [data-hc-request-scope]")
    || (detail.target instanceof Element
      ? detail.target.closest("htmx-request-scope, [data-hc-request-scope]")
      : null);
}

function disablePendingElements(
  state: PendingState,
  trigger: Element,
  scope: Element | null,
  disableMode: string | null,
): void {
  if (disableMode === "none") {
    return;
  }

  if (disableMode === "scope" && scope) {
    const disableSelector = getPendingOption(trigger, scope, "pendingDisableSelector", null)
      || "button, input, select, textarea, a[href], [tabindex]";
    const elements = Array.from(scope.querySelectorAll(disableSelector))
      .filter(function (element) {
        return element !== scope && !element.hasAttribute("data-hc-no-pending-disable");
      });

    elements.forEach(function (element) {
      disableElementForPending(state, element);
    });
    return;
  }

  disableElementForPending(state, trigger);
}

function disableElementForPending(state: PendingState, element: Element): void {
  retainPendingReference(state, element, "disable", function () {
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
      previousTabIndex,
    };
  }, function (previous) {
    if (isDisableableFormElement(element)) {
      element.disabled = previous.previousDisabled;
    } else {
      restoreNullableAttribute(element, "tabindex", previous.previousTabIndex);
    }

    restoreNullableAttribute(element, "aria-disabled", previous.previousAriaDisabled);
    element.classList.remove("hc-pending-disabled");
  });
}

function isDisableableFormElement(element: Element): element is HTMLButtonElement
  | HTMLInputElement
  | HTMLSelectElement
  | HTMLTextAreaElement
  | HTMLOptionElement
  | HTMLOptGroupElement
  | HTMLFieldSetElement {
  return element instanceof HTMLButtonElement
    || element instanceof HTMLInputElement
    || element instanceof HTMLSelectElement
    || element instanceof HTMLTextAreaElement
    || element instanceof HTMLOptionElement
    || element instanceof HTMLOptGroupElement
    || element instanceof HTMLFieldSetElement;
}

function suppressPendingActivation(event: Event): void {
  const target = event.target instanceof Element
    ? event.target.closest(".hc-pending-disabled")
    : null;

  if (!target) {
    return;
  }

  if (event instanceof KeyboardEvent && event.key !== "Enter" && event.key !== " ") {
    return;
  }

  event.preventDefault();
  event.stopImmediatePropagation();
}

function dimStaleRegions(
  state: PendingState,
  trigger: Element,
  scope: Element | null,
  detail: HtmxEventDetail,
): void {
  const selector = getPendingOption(trigger, scope, "staleRegionSelector", null);
  let regions = selector
    ? resolveElementSelectorAll(trigger, selector, scope)
    : scope
      ? Array.from(scope.querySelectorAll("[data-hc-stale-region]"))
      : [];

  if (regions.length === 0 && detail.target instanceof Element) {
    regions = [detail.target];
  }

  regions.forEach(function (region) {
    retainPendingReference(state, region, "stale", function () {
      const previousBusy = region.getAttribute("aria-busy");
      region.classList.add("hc-stale-region");
      region.setAttribute("aria-busy", "true");
      return { previousBusy };
    }, function (previous) {
      region.classList.remove("hc-stale-region");
      restoreNullableAttribute(region, "aria-busy", previous.previousBusy);
    });
  });
}

function showPendingIndicators(state: PendingState, trigger: Element, scope: Element | null): void {
  const selector = getPendingOption(trigger, scope, "requestIndicatorSelector", null);
  const indicators = selector
    ? resolveElementSelectorAll(trigger, selector, scope)
    : scope
      ? Array.from(scope.querySelectorAll("[data-hc-request-indicator]"))
      : [];

  indicators.forEach(function (indicator) {
    retainPendingReference(state, indicator, "indicator", function () {
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
        previousAriaHidden,
      };
    }, function (previous) {
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

function getPendingOption(
  trigger: Element,
  scope: Element | null,
  optionName: string,
  fallback: string | null,
): string | null {
  const attribute = "data-hc-" + optionName.replace(/[A-Z]/g, function (letter) {
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

function retainPendingReference<TPrevious>(
  state: PendingState,
  element: Element,
  key: string,
  apply: () => TPrevious,
  restore: (previous: TPrevious) => void,
): void {
  let references = pendingElementReferences.get(element);
  if (!references) {
    references = new Map();
    pendingElementReferences.set(element, references);
  }

  let entry = references.get(key) as PendingReference<TPrevious> | undefined;
  if (!entry) {
    entry = {
      count: 0,
      previous: apply(),
    };
    references.set(key, entry as PendingReference<unknown>);
  }

  entry.count += 1;
  state.releases.push(function () {
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

function restoreNullableAttribute(element: Element, name: string, value: string | null): void {
  if (value === null) {
    element.removeAttribute(name);
    return;
  }

  element.setAttribute(name, value);
}
