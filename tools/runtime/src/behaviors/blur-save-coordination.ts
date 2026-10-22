import { getHtmxDetail, HtmxEventDetail, HtmxRequestConfig } from "../htmx-events";
import { reportFailure } from "../failures";

const pendingBlurRequests = new Set<object>();
const deferredSaves = new Map<Element, number>();

export function installBlurSaveCoordination(): void {
  document.addEventListener("htmx:beforeSend", function (event) {
    const detail = getHtmxDetail(event);
    if (detail.elt instanceof Element && detail.requestConfig && detail.xhr
      && isBlurRequest(detail.elt, detail.requestConfig)) {
      pendingBlurRequests.add(detail.xhr);
    }
  });

  document.addEventListener("htmx:beforeRequest", function (event) {
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
    document.addEventListener(name, function (event) {
      const xhr = getHtmxDetail(event).xhr;
      if (xhr) {
        pendingBlurRequests.delete(xhr);
      }
    });
  }
}

function deferSave(element: Element, eventType: string | undefined, detail: HtmxEventDetail): void {
  const deadline = Date.now() + 1000;
  function retry(): void {
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

function actionName(config: HtmxRequestConfig): string {
  if (!config.path) {
    return "";
  }
  try {
    return new URL(config.path, window.location.href).pathname.split("/").filter(Boolean).pop() || "";
  } catch {
    return "";
  }
}

function isBlurRequest(element: Element, config: HtmxRequestConfig): boolean {
  const trigger = element.getAttribute("hx-trigger") || element.getAttribute("data-hx-trigger") || "";
  return /(?:^|[,\s])blur(?:$|[,\s\[])/.test(trigger)
    && ["SetValue", "UpdateField", "ValueChanged"].includes(actionName(config));
}

function isSaveRequest(config: HtmxRequestConfig): boolean {
  return ["Save", "Submit", "Update", "Create"].includes(actionName(config));
}
