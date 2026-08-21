import { getRuntimeConfig } from "./config";
import { defineCustomElements } from "./custom-elements";
import { dispatchComponentEvent } from "./events";
import { findBehavior, RuntimeBehaviorName } from "./behaviors/registry";

interface RuntimeState {
  installedBehaviors: Set<string>;
  loadHandlerRegistered: boolean;
}

const runtimeConfig = getRuntimeConfig();
const runtimeState = getRuntimeState();
installConfiguredBehaviors(runtimeConfig.behaviors);
const activeRuntimeConfig = {
  behaviors: Array.from(runtimeState.installedBehaviors) as RuntimeBehaviorName[],
};

function init(root?: Element | Document): void {
  const initRoot = root instanceof Element || root instanceof Document ? root : document;

  for (const behaviorName of activeRuntimeConfig.behaviors) {
    const behavior = findBehavior(behaviorName);
    behavior?.sync?.(initRoot);
  }

  dispatchComponentEvent("htmx-components:load", initRoot, { root: initRoot });
}

defineCustomElements();

window.HtmxComponents = {
  config: activeRuntimeConfig,
  init,
  installedBehaviors: activeRuntimeConfig.behaviors,
};

registerLoadHandler();
init(document.body);

function installConfiguredBehaviors(behaviorNames: RuntimeBehaviorName[]): void {
  for (const name of behaviorNames) {
    const behavior = findBehavior(name);
    if (!behavior) {
      continue;
    }

    if (!runtimeState.installedBehaviors.has(behavior.name)) {
      behavior.install();
      runtimeState.installedBehaviors.add(behavior.name);
    }
  }
}

function registerLoadHandler(): void {
  if (runtimeState.loadHandlerRegistered) {
    return;
  }

  runtimeState.loadHandlerRegistered = true;

  if (window.htmx?.onLoad) {
    window.htmx.onLoad(function (root) {
      window.HtmxComponents?.init(root);
    });
    return;
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () {
      window.HtmxComponents?.init(document.body);
    }, { once: true });
  }
}

function getRuntimeState(): RuntimeState {
  const stateContainer = window as unknown as {
    __htmxComponentsRuntimeState?: RuntimeState;
  };

  if (!stateContainer.__htmxComponentsRuntimeState) {
    stateContainer.__htmxComponentsRuntimeState = {
      installedBehaviors: new Set<string>(),
      loadHandlerRegistered: false,
    };
  }

  return stateContainer.__htmxComponentsRuntimeState;
}
