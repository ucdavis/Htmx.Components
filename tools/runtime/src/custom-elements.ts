import { dispatchComponentEvent } from "./events";
import { dispatchTableConnected } from "./behaviors/table-inline-editing";

export class HtmxTableElement extends HTMLElement {
  connectedCallback(): void {
    dispatchTableConnected(this);
  }
}

export class HtmxRequestScopeElement extends HTMLElement {
  connectedCallback(): void {
    this.dataset.hcRequestScope = this.dataset.hcRequestScope || "";
    dispatchComponentEvent("htmx-components:request-scope-connected", this, { scope: this });
  }
}

export class HtmxErrorRegionElement extends HTMLElement {
  connectedCallback(): void {
    if (!this.hasAttribute("role")) {
      this.setAttribute("role", "status");
    }

    if (!this.hasAttribute("aria-live")) {
      this.setAttribute("aria-live", "polite");
    }
  }

  clear(): void {
    this.hidden = true;
    this.replaceChildren();
  }

  show(message: string): void {
    this.hidden = false;
    this.textContent = message;
  }

  showFragment(fragment: Node): void {
    this.hidden = false;
    this.replaceChildren(fragment);
  }
}

function defineElement(name: string, type: CustomElementConstructor): void {
  if (!window.customElements || customElements.get(name)) {
    return;
  }

  customElements.define(name, type);
}

export function defineCustomElements(): void {
  defineElement("htmx-table", HtmxTableElement);
  defineElement("htmx-request-scope", HtmxRequestScopeElement);
  defineElement("htmx-error-region", HtmxErrorRegionElement);
}
