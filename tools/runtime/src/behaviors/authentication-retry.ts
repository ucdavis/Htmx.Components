import { getHtmxDetail } from "../htmx-events";
import { reportFailure } from "../failures";

interface AuthRetryRequestContext {
  elt?: unknown;
  eventType: string;
  eventClass: ReplayEventClass;
  eventInit: ReplayEventInit;
}

type ReplayEventClass = "MouseEvent" | "KeyboardEvent" | "CustomEvent" | "Event";
type ReplayEventInit = EventInit | MouseEventInit | KeyboardEventInit | CustomEventInit<unknown>;
type ReplayEventFactory = (type: string, eventInitDict?: ReplayEventInit) => Event;

const authRetryRequestContexts = new WeakMap<object, AuthRetryRequestContext>();
let authRetryInProgress = false;

export function installAuthenticationRetry(): void {
  document.body.addEventListener("htmx:beforeRequest", function (event) {
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
      eventInit: getEventInit(triggeringEvent),
    });
  });

  document.body.addEventListener("htmx:responseError", async function (event) {
    const detail = getHtmxDetail(event);
    const xhr = detail.xhr;
    if (!xhr) {
      return;
    }

    const failureHeader = typeof xhr.getResponseHeader === "function"
      ? xhr.getResponseHeader("X-Auth-Failure")
      : null;
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

      const loginResult = await new Promise<"success" | "closed" | "timeout">(function (resolve) {
        let completed = false;
        const timeoutMs = 30000;
        let closeTimer = 0;
        let timeoutTimer = 0;

        function finish(result: "success" | "closed" | "timeout"): void {
          if (completed) {
            return;
          }

          completed = true;
          window.removeEventListener("message", listener);
          window.clearInterval(closeTimer);
          window.clearTimeout(timeoutTimer);
          resolve(result);
        }

        function listener(messageEvent: MessageEvent): void {
          if (messageEvent.data === "login-success" && messageEvent.origin === loginOrigin && messageEvent.source === popup) {
            finish("success");
          }
        }

        closeTimer = window.setInterval(function () {
          if (popup.closed) {
            finish("closed");
          }
        }, 250);
        timeoutTimer = window.setTimeout(function () {
          finish("timeout");
        }, timeoutMs);

        window.addEventListener("message", listener);
      });

      if (loginResult !== "success") {
        reportFailure(detail, "Sign in required", loginResult === "closed"
          ? "The sign-in popup was closed. Please try again."
          : "Sign-in timed out. Please try again.");
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

  document.body.addEventListener("htmx:afterRequest", function (event) {
    const xhr = getHtmxDetail(event).xhr;
    if (xhr) {
      authRetryRequestContexts.delete(xhr);
    }
  });
}

function getEventInit(event: Event): ReplayEventInit {
  const base: EventInit = {
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

function getReplayEventClass(event: Event): ReplayEventClass {
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

function getReplayEventFactory(eventClass: ReplayEventClass): ReplayEventFactory {
  switch (eventClass) {
    case "MouseEvent": return (type, init) => new MouseEvent(type, init as MouseEventInit);
    case "KeyboardEvent": return (type, init) => new KeyboardEvent(type, init as KeyboardEventInit);
    case "CustomEvent": return (type, init) => new CustomEvent(type, init as CustomEventInit<unknown>);
    default: return (type, init) => new Event(type, init);
  }
}
