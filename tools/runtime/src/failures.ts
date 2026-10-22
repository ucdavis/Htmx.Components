import { HtmxEventDetail } from "./htmx-events";

type FailureReporter = (detail: HtmxEventDetail, title: string, message: string) => void;

// Behaviors installed by separate bundle references must share the same reporter.
const container = window as unknown as { __htmxComponentsFailureReporter?: FailureReporter };

export function setFailureReporter(value: FailureReporter): void {
  container.__htmxComponentsFailureReporter = value;
}

export function reportFailure(detail: HtmxEventDetail, title: string, message: string): void {
  if (container.__htmxComponentsFailureReporter) {
    container.__htmxComponentsFailureReporter(detail, title, message);
  } else {
    console.warn(`${title}: ${message}`);
  }
}
