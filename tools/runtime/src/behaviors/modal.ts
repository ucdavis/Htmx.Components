import { getHtmxDetail, HtmxEventDetail } from "../htmx-events";

interface ModalElement extends HTMLElement {
  open?: boolean;
  showModal?: () => void;
  close?: () => void;
  hcModalOpener?: Element | null;
  hcModalRestoreFocus?: boolean;
}

let activeModal: ModalElement | null = null;

export function syncModals(root: Element | Document): void {
  const modals: Element[] = [];
  const closestModal = root instanceof Element
    ? root.closest("[data-hc-modal]")
    : null;

  if (closestModal) {
    modals.push(closestModal);
  }

  if (root instanceof Element && root.matches("[data-hc-modal]") && root !== closestModal) {
    modals.push(root);
  }

  modals.push(...Array.from(root.querySelectorAll("[data-hc-modal]"))
    .filter((modal) => !modals.includes(modal)));

  modals.forEach((modal) => prepareModal(asModal(modal)));
}

export function installModalBehavior(): void {
  document.addEventListener("click", function (event) {
    const closeTrigger = event.target instanceof Element
      ? event.target.closest("[data-hc-modal-close]")
      : null;

    if (!closeTrigger) {
      return;
    }

    const modal = closeTrigger.closest("[data-hc-modal]");
    if (!modal) {
      return;
    }

    closeModal(asModal(modal));
  });

  document.addEventListener("htmx:afterSwap", function (event) {
    const detail = getHtmxDetail(event);
    const trigger = resolveHtmxRequestTrigger(detail);
    const target = detail.target;

    if (!(trigger instanceof Element) || !(target instanceof Element)) {
      return;
    }

    const modalId = trigger.getAttribute("data-hc-open-modal");
    if (!modalId) {
      return;
    }

    const modal = document.getElementById(modalId);
    if (!modal || !modal.matches("[data-hc-modal]") || !requestTargetedModalBody(trigger, modal, target)) {
      return;
    }

    openModal(asModal(modal), trigger);
  });
}

function prepareModal(modal: ModalElement): void {
  if (modal.dataset.hcModalPrepared === "true") {
    return;
  }

  modal.dataset.hcModalPrepared = "true";
  modal.addEventListener("close", function () {
    handleModalClosed(modal);
  });
}

function resolveHtmxRequestTrigger(detail: HtmxEventDetail): Element | null {
  const requestElement = detail.requestConfig?.elt;
  if (requestElement instanceof Element) {
    return requestElement;
  }

  return detail.elt instanceof Element ? detail.elt : null;
}

function requestTargetedModalBody(trigger: Element, modal: Element, target: Element): boolean {
  const configuredTarget = modal.getAttribute("data-hc-modal-body-target");
  const targetSelector = trigger.getAttribute("hx-target") || trigger.getAttribute("data-hx-target");
  const body = configuredTarget
    ? document.querySelector(configuredTarget)
    : modal.querySelector("[data-hc-modal-body]");

  if (!body || target !== body) {
    return false;
  }

  return !targetSelector || targetSelector === configuredTarget || targetSelector === `#${body.id}`;
}

function openModal(modal: ModalElement, opener: Element): void {
  prepareModal(modal);

  if (activeModal && activeModal !== modal) {
    closeModal(activeModal, { restoreFocus: false });
  }

  modal.hcModalOpener = opener;
  activeModal = modal;

  if (typeof modal.showModal === "function" && !modal.open) {
    modal.showModal();
    return;
  }

  if (!modal.open) {
    modal.setAttribute("open", "");
  }
}

function closeModal(modal: ModalElement, options?: { restoreFocus?: boolean }): void {
  const settings = options || {};
  modal.hcModalRestoreFocus = settings.restoreFocus !== false;

  if (typeof modal.close === "function" && modal.open) {
    modal.close();
    return;
  }

  modal.removeAttribute("open");
  handleModalClosed(modal);
}

function handleModalClosed(modal: ModalElement): void {
  const bodySelector = modal.getAttribute("data-hc-modal-body-target");
  const body = bodySelector
    ? document.querySelector(bodySelector)
    : modal.querySelector("[data-hc-modal-body]");

  if (body) {
    body.replaceChildren();
  }

  if (activeModal === modal) {
    activeModal = null;
  }

  const opener = modal.hcModalOpener;
  const shouldRestoreFocus = modal.hcModalRestoreFocus !== false;
  modal.hcModalOpener = null;
  modal.hcModalRestoreFocus = true;

  if (shouldRestoreFocus && opener instanceof HTMLElement && opener.isConnected) {
    opener.focus();
  }
}

function asModal(modal: Element): ModalElement {
  return modal as ModalElement;
}
