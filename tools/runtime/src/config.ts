import { defaultBehaviors, RuntimeBehaviorName } from "./behaviors/registry";

export const configElementId = "htmx-components-runtime-config";

interface RawRuntimeConfig {
  behaviors?: unknown;
}

export interface RuntimeConfig {
  behaviors: RuntimeBehaviorName[];
}

export function getRuntimeConfig(): RuntimeConfig {
  const element = document.getElementById(configElementId);
  if (!element) {
    return { behaviors: defaultBehaviors };
  }

  try {
    const config = JSON.parse(element.textContent || "{}") as RawRuntimeConfig;
    return {
      behaviors: Array.isArray(config.behaviors)
        ? normalizeBehaviors(config.behaviors)
        : defaultBehaviors,
    };
  } catch (error) {
    console.warn("Htmx.Components runtime config could not be parsed.", error);
    return { behaviors: defaultBehaviors };
  }
}

export function behaviorEnabled(runtimeConfig: RuntimeConfig, name: RuntimeBehaviorName): boolean {
  return runtimeConfig.behaviors.includes(name);
}

function normalizeBehaviors(behaviors: unknown[]): RuntimeBehaviorName[] {
  return behaviors.filter((behavior): behavior is RuntimeBehaviorName =>
    typeof behavior === "string" && defaultBehaviors.includes(behavior as RuntimeBehaviorName));
}
