import { getHtmxDetail } from "../htmx-events";

interface AuthRetryRequestContext {
  elt?: unknown;
  eventType: string;
  eventClass: string;
  eventInit: ReplayEventInit;
}

type ReplayEventInit = EventInit | MouseEventInit | KeyboardEventInit | CustomEventInit<unknown>;
type ReplayEventConstructor = new (type: string, eventInitDict?: ReplayEventInit) => Event;

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
      eventClass: triggeringEvent.constructor.name,
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
      const popup = window.open(loginUrl, "authPopup", "width=600,height=700");

      const loginSuccess = await new Promise<boolean>(function (resolve) {
        let completed = false;
        const timeoutMs = 30000;
        let closeTimer = 0;
        let timeoutTimer = 0;

        function finish(success: boolean): void {
          if (completed) {
            return;
          }

          completed = true;
          window.removeEventListener("message", listener);
          window.clearInterval(closeTimer);
          window.clearTimeout(timeoutTimer);
          resolve(success);
        }

        function listener(messageEvent: MessageEvent): void {
          if (messageEvent.data === "login-success") {
            finish(true);
          }
        }

        closeTimer = window.setInterval(function () {
          if (popup?.closed) {
            finish(false);
          }
        }, 250);
        timeoutTimer = window.setTimeout(function () {
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

function getEventConstructor(eventClass: string): ReplayEventConstructor {
  const constructors = window as unknown as Record<string, ReplayEventConstructor | undefined>;
  return constructors[eventClass] || Event;
}
