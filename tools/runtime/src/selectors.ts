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
    const result = guardSelector(selector, () => source.closest(selector.substring("closest ".length)), null);
    return result ? [result] : [];
  }

  if (selector.startsWith("find ")) {
    const root = scope || source;
    return querySelectorAll(root, selector.substring("find ".length));
  }

  const root = scope || document;
  return querySelectorAll(root, selector);
}

export function querySelectorAll(root: Element | Document, selector: string): Element[] {
  return guardSelector(selector, () => Array.from(root.querySelectorAll(selector)), []);
}

export function querySelector(root: Element | Document, selector: string): Element | null {
  return guardSelector(selector, () => root.querySelector(selector), null);
}

function guardSelector<T>(selector: string, query: () => T, fallback: T): T {
  try {
    return query();
  } catch (error) {
    if (!(error instanceof DOMException) || error.name !== "SyntaxError") {
      throw error;
    }
    console.warn(`Htmx.Components ignored invalid selector: ${selector}`, error);
    return fallback;
  }
}

export function collectMatches(root: Element | Document, selector: string): Element[] {
  const closest = root instanceof Element ? root.closest(selector) : null;
  return Array.from(new Set([...(closest ? [closest] : []), ...root.querySelectorAll(selector)]));
}
