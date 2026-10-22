import { installAuthenticationRetry } from "./authentication-retry";
import { installBlurSaveCoordination } from "./blur-save-coordination";
import { installErrorHandling } from "./error-handling";
import { installModalBehavior, syncModals } from "./modal";
import { installPageStateHeaders } from "./page-state-headers";
import { installRequestLifecycleUx } from "./request-lifecycle";
import { installTableInlineEditing, syncTables } from "./table-inline-editing";

export interface RuntimeBehavior {
  name: string;
  install: () => void;
  sync?: (root: Element | Document) => void;
}

export const runtimeBehaviors = [
  {
    name: "page-state-headers",
    install: installPageStateHeaders,
  },
  {
    name: "table-inline-editing",
    install: installTableInlineEditing,
    sync: syncTables,
  },
  {
    name: "blur-save-coordination",
    install: installBlurSaveCoordination,
  },
  {
    name: "request-lifecycle",
    install: installRequestLifecycleUx,
  },
  {
    name: "error-handling",
    install: installErrorHandling,
  },
  {
    name: "authentication-retry",
    install: installAuthenticationRetry,
  },
  {
    name: "modal",
    install: installModalBehavior,
    sync: syncModals,
  },
] as const satisfies readonly RuntimeBehavior[];

export type RuntimeBehaviorName = typeof runtimeBehaviors[number]["name"];

export const defaultBehaviors: RuntimeBehaviorName[] = runtimeBehaviors.map((behavior) => behavior.name);

export function findBehavior(name: string): RuntimeBehavior | undefined {
  return runtimeBehaviors.find((behavior) => behavior.name === name);
}
