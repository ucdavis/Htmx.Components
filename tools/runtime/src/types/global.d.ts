declare global {
  interface HtmxExtension {
    isInlineSwap?: () => boolean;
  }

  interface HtmxRuntime {
    defineExtension(name: string, extension: HtmxExtension): void;
    onLoad(callback: (root: Element | Document) => void): void;
    trigger(element: Element, eventType: string): void;
  }

  interface Window {
    htmx?: HtmxRuntime;
    HtmxComponents?: {
      config: {
        scripts: string[];
      };
      init(root?: Element | Document): void;
    };
  }
}

export {};
