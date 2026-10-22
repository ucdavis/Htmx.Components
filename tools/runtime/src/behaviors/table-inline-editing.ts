import { collectMatches } from "../selectors";
import { getHtmxDetail } from "../htmx-events";

export function installTableInlineEditing(): void {
  if (window.htmx) {
    window.htmx.defineExtension("tableinline", {
      isInlineSwap: function () {
        return true;
      },
    });
  }

  document.addEventListener("htmx-components:table-connected", function (event) {
    const table = event.target;
    if (table instanceof Element) {
      syncTableEditing(table);
    }
  });

  document.addEventListener("htmx:afterSettle", function (event) {
    const detail = getHtmxDetail(event);
    syncTables(detail.target instanceof Element ? detail.target : document);
  });
}

export function syncTables(root: Element | Document): void {
  collectMatches(root, "[data-hc-table-component]").forEach(syncTableEditing);
}

export function syncTableEditing(table: Element): void {
  const component = table.closest("[data-hc-table-component]") || table;
  const toggle = component.querySelector("[data-hc-table-edit-toggle]");

  if (!toggle) {
    return;
  }

  component.classList.toggle("editing-mode", toggle.classList.contains("editing-mode"));
}
