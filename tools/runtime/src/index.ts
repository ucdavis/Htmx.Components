import { getRuntimeConfig } from "./config";
import { defineCustomElements } from "./custom-elements";
import { dispatchComponentEvent } from "./events";
import { findBehavior, RuntimeBehaviorName } from "./behaviors/registry";
import { getHtmxDetail } from "./htmx-events";

interface RuntimeState {
  installedBehaviors: Set<RuntimeBehaviorName>;
  requestedBehaviors: Set<RuntimeBehaviorName>;
  loadHandlerRegistered: boolean;
  startupScheduled: boolean;
  initialBody?: Element;
  warnedMissingHtmx: boolean;
}

const runtimeState = getRuntimeState();
for (const name of getRuntimeConfig().behaviors) {
  runtimeState.requestedBehaviors.add(name);
}
const activeRuntimeConfig = { behaviors: Array.from(runtimeState.installedBehaviors) };

function init(root?: Element | Document): void {
  const initRoot = root instanceof Element || root instanceof Document ? root : document;
  for (const name of runtimeState.installedBehaviors) {
    findBehavior(name)?.sync?.(initRoot);
  }
  dispatchComponentEvent("htmx-components:load", initRoot, { root: initRoot });
}

window.HtmxComponents = {
  config: activeRuntimeConfig,
  init,
  installedBehaviors: activeRuntimeConfig.behaviors,
};

// Listen on document so early head loading does not depend on a body existing.
if (!runtimeState.loadHandlerRegistered) {
  runtimeState.loadHandlerRegistered = true;
  document.addEventListener("htmx:load", function (event) {
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

function start(): void {
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

function getRuntimeState(): RuntimeState {
  const container = window as unknown as { __htmxComponentsRuntimeState?: RuntimeState };
  return container.__htmxComponentsRuntimeState ??= {
    installedBehaviors: new Set(),
    requestedBehaviors: new Set(),
    loadHandlerRegistered: false,
    startupScheduled: false,
    warnedMissingHtmx: false,
  };
}
