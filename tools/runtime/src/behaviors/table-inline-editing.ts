import { dispatchComponentEvent } from "../events";
import { getHtmxDetail } from "../htmx-events";

export function installTableInlineEditing(): void {
  if (window.htmx) {
    window.htmx.defineExtension("tableinline", {
      isInlineSwap: function () {
        return true;
      },
    });
  }

  document.addEventListener("htmx:afterSettle", function (event) {
    const detail = getHtmxDetail(event);
    syncTables(detail.target instanceof Element ? detail.target : document);
  });
}

export function syncTables(root: Element | Document): void {
  const tables: Element[] = [];
  const closestTable = root instanceof Element
    ? root.closest("[data-hc-table-component]")
    : null;

  if (closestTable) {
    tables.push(closestTable);
  }

  if (root instanceof Element && root.matches("[data-hc-table-component]") && root !== closestTable) {
    tables.push(root);
  }

  tables.push(...Array.from(root.querySelectorAll("[data-hc-table-component]"))
    .filter((table) => !tables.includes(table)));

  tables.forEach(syncTableEditing);
}

export function syncTableEditing(table: Element): void {
  const component = table.closest("[data-hc-table-component]") || table;
  const toggle = component.querySelector("[data-hc-table-edit-toggle]");

  if (!toggle) {
    return;
  }

  component.classList.toggle("editing-mode", toggle.classList.contains("editing-mode"));
}

export function dispatchTableConnected(table: Element): void {
  syncTableEditing(table);
  dispatchComponentEvent("htmx-components:table-connected", table, { table });
}
