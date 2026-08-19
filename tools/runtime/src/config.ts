export const configElementId = "htmx-components-config";

export const defaultScripts = [
  "page-state-headers",
  "table-inline-editing",
  "blur-save-coordination",
  "request-lifecycle",
  "error-handling",
  "authentication-retry",
  "modal",
];

interface RawRuntimeConfig {
  Scripts?: unknown;
  scripts?: unknown;
}

export interface RuntimeConfig {
  scripts: string[];
}

export function getRuntimeConfig(): RuntimeConfig {
  const element = document.getElementById(configElementId);
  if (!element) {
    return { scripts: defaultScripts };
  }

  try {
    const config = JSON.parse(element.textContent || "{}") as RawRuntimeConfig;
    return {
      scripts: Array.isArray(config.Scripts)
        ? config.Scripts
        : Array.isArray(config.scripts)
          ? config.scripts
          : defaultScripts,
    };
  } catch (error) {
    console.warn("Htmx.Components runtime config could not be parsed.", error);
    return { scripts: defaultScripts };
  }
}

export function scriptEnabled(runtimeConfig: RuntimeConfig, name: string): boolean {
  return runtimeConfig.scripts.includes(name);
}
