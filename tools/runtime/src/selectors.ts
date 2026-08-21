export function resolveElementSelector(
  source: Element,
  selector: string | null | undefined,
  scope?: Element | Document | null,
): Element | null {
  return resolveElementSelectorAll(source, selector, scope)[0] || null;
}

export function resolveElementSelectorAll(
  source: Element,
  selector: string | null | undefined,
  scope?: Element | Document | null,
): Element[] {
  if (!selector) {
    return [];
  }

  if (selector === "this") {
    return [source];
  }

  if (selector.startsWith("closest ")) {
    const result = source.closest(selector.substring("closest ".length));
    return result ? [result] : [];
  }

  if (selector.startsWith("find ")) {
    const root = scope || source;
    return Array.from(root.querySelectorAll(selector.substring("find ".length)));
  }

  const root = scope || document;
  return Array.from(root.querySelectorAll(selector));
}
