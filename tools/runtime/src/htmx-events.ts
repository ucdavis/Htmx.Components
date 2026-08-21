export interface HtmxRequestConfig {
  elt?: unknown;
  path?: string;
  triggeringEvent?: Event;
}

export interface HtmxEventDetail {
  elt?: unknown;
  requestConfig?: HtmxRequestConfig;
  target?: unknown;
  xhr?: HtmxXhr;
}

export interface HtmxXhr {
  status?: number;
  responseText?: string;
  getResponseHeader?: (name: string) => string | null;
}

export type HtmxCustomEvent = CustomEvent<HtmxEventDetail>;

export function getHtmxDetail(event: Event): HtmxEventDetail {
  return event instanceof CustomEvent && typeof event.detail === "object" && event.detail !== null
    ? event.detail as HtmxEventDetail
    : {};
}
