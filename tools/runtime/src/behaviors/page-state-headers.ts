import { getHtmxDetail } from "../htmx-events";

export function installPageStateHeaders(): void {
  document.addEventListener("htmx:configRequest", function (event) {
    const pageStateInput = document.querySelector<HTMLInputElement>('input[name="page_state"]');

    if (!pageStateInput?.value) {
      return;
    }

    const detail = getHtmxDetail(event) as { headers?: Record<string, string> };
    if (!detail.headers) {
      return;
    }

    detail.headers["X-Page-State"] = pageStateInput.value;
  });
}
