import { getHtmxDetail, HtmxRequestConfig } from "../htmx-events";

const pendingBlurRequests = new Set<Element>();

interface DeferredRequest {
  element: Element;
  eventType?: string;
}

export function installBlurSaveCoordination(): void {
  document.addEventListener("htmx:beforeRequest", function (event) {
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
      eventType: requestConfig.triggeringEvent?.type,
    });
  });

  document.addEventListener("htmx:afterRequest", cleanupBlurRequest);
  document.addEventListener("htmx:responseError", cleanupBlurRequest);
}

function retryAfterBlur(request: DeferredRequest): void {
  const maxRetries = 40;
  let retryCount = 0;
  const element = request.element;
  const eventType = request.eventType || (element instanceof HTMLFormElement ? "submit" : "click");

  const retry = function () {
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

function replayDeferredRequest(element: Element, eventType: string): void {
  if (!element.isConnected) {
    return;
  }

  window.htmx?.trigger(element, element instanceof HTMLFormElement ? "submit" : eventType);
}

function cleanupBlurRequest(event: Event): void {
  const detail = getHtmxDetail(event);
  const element = detail.elt;
  const requestConfig = detail.requestConfig;

  if (element instanceof Element && requestConfig && isBlurRequest(element, requestConfig)) {
    pendingBlurRequests.delete(element);
  }
}

function isBlurRequest(element: Element, requestConfig: HtmxRequestConfig): boolean {
  return element.hasAttribute("hx-trigger")
    && (element.getAttribute("hx-trigger") || "").includes("blur")
    && !!requestConfig.path
    && (
      requestConfig.path.includes("/SetValue")
      || requestConfig.path.includes("/UpdateField")
      || requestConfig.path.includes("/ValueChanged")
    );
}

function isSaveRequest(requestConfig: HtmxRequestConfig): boolean {
  return !!requestConfig.path
    && (
      requestConfig.path.includes("/Save")
      || requestConfig.path.includes("/Submit")
      || requestConfig.path.includes("/Update")
      || requestConfig.path.includes("/Create")
    );
}
