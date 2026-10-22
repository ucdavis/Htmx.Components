import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';

const files = new Map([
  ['/htmx.js', new URL('../../node_modules/htmx.org/dist/htmx.min.js', import.meta.url)],
  ['/runtime.js', new URL('../../wwwroot/js/htmx-components.js', import.meta.url)],
  ['/wwwroot/js/htmx-components.js', new URL('../../wwwroot/js/htmx-components.js', import.meta.url)],
  ['/smoke', new URL('./request-lifecycle-smoke.html', import.meta.url)],
]);

function fixture(url) {
  const mode = url.searchParams.get('mode') || 'deferred';
  const behaviors = url.searchParams.get('behaviors');
  const config = behaviors === null ? {} : { behaviors: behaviors.split(',').filter(Boolean) };
  const runtime = `<script src="/runtime.js" ${mode === 'early' ? '' : 'defer'}></script>`;
  return `<!doctype html><html><head><meta charset="utf-8">
    <script>
      window.bodyLoads = 0; window.componentLoads = 0;
      document.addEventListener('htmx-components:load', e => {
        window.componentLoads++; if (e.detail.root === document.body) window.bodyLoads++;
      });
    </script>
    <script type="application/json" id="htmx-components-runtime-config">${JSON.stringify(config)}</script>
    ${mode === 'missing' ? '' : '<script src="/htmx.js"></script>'}
    ${mode === 'late' ? '' : runtime}
    ${mode === 'duplicate' || mode === 'missing' ? runtime : ''}
    </head><body>
    <input type="hidden" name="page_state" value="shared-state">
    <htmx-request-scope id="scope" data-hc-pending-disable="none">
      <htmx-error-region id="errors" data-hc-error-region hidden></htmx-error-region>
      <input id="field" hx-post="/api/UpdateField" hx-trigger="blur" hx-swap="none">
      <input id="field2" hx-post="/api/SetValue" hx-trigger="blur" hx-swap="none">
      <button id="save" hx-post="/api/Save" hx-swap="none">Save</button>
      <button id="request" hx-get="/api/content" hx-target="#target">Request</button>
      <div id="target"></div>
    </htmx-request-scope>
    <button id="open-one" hx-get="/api/modal" hx-target="#body-one" data-hc-open-modal="modal-one">Open one</button>
    <button id="open-two" hx-get="/api/modal" hx-target="#body-two" data-hc-open-modal="modal-two">Open two</button>
    <dialog id="modal-one" data-hc-modal data-hc-modal-body-target="#body-one">
      <button data-hc-modal-close>Close</button><div id="body-one" data-hc-modal-body></div>
    </dialog>
    <dialog id="modal-two" data-hc-modal data-hc-modal-body-target="#body-two">
      <button data-hc-modal-close>Close</button><div id="body-two" data-hc-modal-body></div>
    </dialog>
    <div id="table" data-hc-table-component><button data-hc-table-edit-toggle class="editing-mode">Toggle</button></div>
    ${mode === 'late' ? `<script>window.addEventListener('load', () => { const s = document.createElement('script'); s.src='/runtime.js'; document.head.append(s); });</script>` : ''}
    </body></html>`;
}

const server = createServer(async (request, response) => {
  const url = new URL(request.url, 'http://127.0.0.1');
  response.setHeader('Cache-Control', 'no-store');
  try {
    if (files.has(url.pathname)) {
      response.setHeader('Content-Type', url.pathname === '/smoke' ? 'text/html' : 'text/javascript');
      response.end(await readFile(files.get(url.pathname)));
    } else if (url.pathname === '/fixture') {
      response.setHeader('Content-Type', 'text/html');
      response.end(fixture(url));
    } else if (url.pathname === '/popup') {
      response.setHeader('Content-Type', 'text/html');
      response.end('<!doctype html><title>Sign in</title><p>Popup fixture</p>');
    } else if (url.pathname.startsWith('/api/')) {
      response.setHeader('Content-Type', 'text/html');
      response.end(url.pathname === '/api/modal' ? '<p>Modal content</p>' : '<span id="loaded">Loaded</span>');
    } else {
      response.writeHead(404).end();
    }
  } catch (error) {
    console.error(error);
    response.writeHead(500).end();
  }
});
server.listen(4173, '127.0.0.1');
