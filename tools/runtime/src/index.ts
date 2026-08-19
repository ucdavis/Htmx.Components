import { getRuntimeConfig, scriptEnabled } from "./config";
import { defineCustomElements } from "./custom-elements";
import { dispatchComponentEvent } from "./events";
import { installAuthenticationRetry } from "./behaviors/authentication-retry";
import { installBlurSaveCoordination } from "./behaviors/blur-save-coordination";
import { installErrorHandling } from "./behaviors/error-handling";
import { installModalBehavior, syncModals } from "./behaviors/modal";
import { installPageStateHeaders } from "./behaviors/page-state-headers";
import { installRequestLifecycleUx } from "./behaviors/request-lifecycle";
import { installTableInlineEditing, syncTables } from "./behaviors/table-inline-editing";

const runtimeConfig = getRuntimeConfig();

function init(root?: Element | Document): void {
  const initRoot = root instanceof Element || root instanceof Document ? root : document;

  syncTables(initRoot);
  syncModals(initRoot);
  dispatchComponentEvent("htmx-components:load", initRoot, { root: initRoot });
}

defineCustomElements();

if (scriptEnabled(runtimeConfig, "page-state-headers")) {
  installPageStateHeaders();
}

if (scriptEnabled(runtimeConfig, "table-inline-editing")) {
  installTableInlineEditing();
}

if (scriptEnabled(runtimeConfig, "blur-save-coordination")) {
  installBlurSaveCoordination();
}

if (scriptEnabled(runtimeConfig, "request-lifecycle")) {
  installRequestLifecycleUx();
}

if (scriptEnabled(runtimeConfig, "error-handling")) {
  installErrorHandling();
}

if (scriptEnabled(runtimeConfig, "authentication-retry")) {
  installAuthenticationRetry();
}

if (scriptEnabled(runtimeConfig, "modal")) {
  installModalBehavior();
}

window.HtmxComponents = {
  config: runtimeConfig,
  init,
};

if (window.htmx?.onLoad) {
  window.htmx.onLoad(init);
} else if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", function () {
    init(document.body);
  }, { once: true });
} else {
  init(document.body);
}
