export function dispatchComponentEvent(
  name: string,
  target: Element | Document | unknown,
  detail: Record<string, unknown>,
): void {
  const eventTarget = target instanceof Element || target instanceof Document ? target : document;
  eventTarget.dispatchEvent(new CustomEvent(name, {
    bubbles: true,
    detail,
  }));
}
