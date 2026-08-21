import { getHtmxDetail, HtmxEventDetail } from "../htmx-events";

interface StatusError {
  title: string;
  message: string;
}

interface ErrorRegionElement extends Element {
  hidden: boolean;
  clear?: () => void;
  showFragment?: (fragment: Node) => void;
}

export function installErrorHandling(): void {
  ensureGlobalErrorRegion();

  document.addEventListener("htmx:beforeRequest", clearRequestError);
  document.addEventListener("htmx:responseError", showResponseError);
  document.addEventListener("htmx:sendError", function (event) {
    showClientError(event, "Request failed", "The request could not be sent. Check your connection and try again.");
  });
  document.addEventListener("htmx:timeout", function (event) {
    showClientError(event, "Request timed out", "The request took too long. Please try again.");
  });
  document.addEventListener("htmx:swapError", function (event) {
    showClientError(event, "Display failed", "The response could not be displayed. Please try again.");
  });
}

function clearRequestError(event: Event): void {
  const region = findErrorRegion(getHtmxDetail(event));

  if (region) {
    clearErrorRegion(region);
  }
}

function showResponseError(event: Event): void {
  const detail = getHtmxDetail(event);
  const xhr = detail.xhr;
  const failureHeader = typeof xhr?.getResponseHeader === "function"
    ? xhr.getResponseHeader("X-Auth-Failure")
    : null;

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

function showClientError(event: Event, title: string, message: string): void {
  showErrorFragment(getHtmxDetail(event), createErrorFragment(title, message));
}

function showErrorFragment(detail: HtmxEventDetail, fragment: Node): void {
  const region = findErrorRegion(detail) || ensureGlobalErrorRegion();

  if (typeof region.showFragment === "function") {
    region.showFragment(fragment);
    return;
  }

  region.hidden = false;
  region.replaceChildren(fragment);
}

function clearErrorRegion(region: ErrorRegionElement): void {
  if (typeof region.clear === "function") {
    region.clear();
    return;
  }

  region.hidden = true;
  region.replaceChildren();
}

function parseErrorFragment(responseText: string | undefined): Node | null {
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

function createErrorFragment(title: string, message: string): Node {
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

function getStatusError(status: number): StatusError {
  if (status === 400) {
    return {
      title: "Request not completed",
      message: "The request could not be completed. Check your entry and try again.",
    };
  }

  if (status === 401) {
    return {
      title: "Sign in required",
      message: "Please sign in and try again.",
    };
  }

  if (status === 403) {
    return {
      title: "Access denied",
      message: "You do not have permission to perform this action.",
    };
  }

  if (status >= 500) {
    return {
      title: "Something went wrong",
      message: "The request could not be completed. Please try again.",
    };
  }

  return {
    title: "Request failed",
    message: "The request could not be completed. Please try again.",
  };
}

function findErrorRegion(detail: HtmxEventDetail): ErrorRegionElement | null {
  const trigger = detail.elt instanceof Element ? detail.elt : null;
  const target = detail.target instanceof Element ? detail.target : null;
  const scope = trigger?.closest("htmx-request-scope, [data-hc-request-scope], [data-hc-table-component]")
    || target?.closest("htmx-request-scope, [data-hc-request-scope], [data-hc-table-component]")
    || null;

  return asErrorRegion(scope?.querySelector("htmx-error-region, [data-hc-error-region]")
      || document.querySelector("htmx-error-region[data-hc-global-error-region], [data-hc-global-error-region]"));
}

function ensureGlobalErrorRegion(): ErrorRegionElement {
  let region = document.querySelector("htmx-error-region[data-hc-global-error-region], [data-hc-global-error-region]");

  if (region) {
    return asErrorRegion(region)!;
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
  return asErrorRegion(region)!;
}

function asErrorRegion(region: Element | null): ErrorRegionElement | null {
  return region as ErrorRegionElement | null;
}
