import { test, expect } from '@playwright/test';

async function boot(page, query = '') {
  await page.goto(`/fixture${query}`);
  await expect.poll(() => page.evaluate(() => window.bodyLoads)).toBe(1);
}
async function emit(page, name, detail = {}) {
  await page.evaluate(({ name, detail }) => {
    const elt = document.getElementById(detail.id || 'request');
    elt.dispatchEvent(new CustomEvent(name, { bubbles: true, cancelable: true, detail: { ...detail, elt, target: document.getElementById('target') } }));
  }, { name, detail });
}
async function startBlur(page, id = 'field') {
  await page.locator(`#${id}`).focus();
  await page.locator(`#${id}`).evaluate(e => e.blur());
}
async function observeRequests(page) {
  const paths = [];
  page.on('request', request => {
    const path = new URL(request.url()).pathname;
    if (path.startsWith('/api/')) paths.push(path);
  });
  return paths;
}

for (const mode of ['deferred', 'early', 'late', 'duplicate']) {
  test(`startup initializes once: ${mode}`, async ({ page }) => {
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await boot(page, `?mode=${mode}`);
    await expect(page.locator('#table')).toHaveClass('editing-mode');
    await page.waitForTimeout(50); // Allow HTMX's asynchronous initial body-load event.
    expect(await page.evaluate(() => window.bodyLoads)).toBe(1);
    await page.evaluate(() => window.HtmxComponents.init(document.body));
    expect(await page.evaluate(() => window.bodyLoads)).toBe(2);
    await page.locator('#request').click();
    await expect(page.locator('#loaded')).toHaveText('Loaded');
    await expect.poll(() => page.evaluate(() => window.componentLoads)).toBeGreaterThan(2);
    expect(errors).toEqual([]);
  });
}

test('missing HTMX warns once, including duplicate runtime loading', async ({ page }) => {
  const warnings = [];
  page.on('console', message => { if (message.type() === 'warning') warnings.push(message.text()); });
  await boot(page, '?mode=missing');
  expect(warnings.filter(x => x.includes('requires HTMX'))).toHaveLength(1);
});

test('blur request sends, save waits and duplicate saves replay only once', async ({ page }) => {
  let blur;
  await page.route('**/api/UpdateField', route => { blur = route; });
  await boot(page);
  const paths = await observeRequests(page);
  await startBlur(page);
  await expect.poll(() => !!blur).toBe(true);
  await page.locator('#save').click({ clickCount: 2 });
  expect(paths.filter(x => x === '/api/Save')).toHaveLength(0);
  await blur.fulfill({ status: 204 });
  await expect.poll(() => paths.filter(x => x === '/api/Save').length).toBe(1);
});

test('coordination waits for every concurrent request', async ({ page }) => {
  const routes = [];
  await page.route(/\/api\/(?:SetValue|UpdateField)$/, route => { routes.push(route); });
  await boot(page);
  const paths = await observeRequests(page);
  await startBlur(page, 'field');
  await startBlur(page, 'field2');
  await expect.poll(() => routes.length).toBe(2);
  await page.locator('#save').click();
  await routes[0].fulfill({ status: 204 });
  await page.waitForTimeout(60);
  expect(paths.filter(x => x === '/api/Save')).toHaveLength(0);
  await routes[1].fulfill({ status: 204 });
  await expect.poll(() => paths.filter(x => x === '/api/Save').length).toBe(1);
});

for (const outcome of ['success', 'removed', 'changed trigger', 'HTTP error', 'network error', 'abort', 'timeout']) {
  test(`blur cleanup permits save after ${outcome}`, async ({ page }) => {
    let blur;
    await page.route('**/api/UpdateField', route => { blur = route; });
    await boot(page);
    const paths = await observeRequests(page);
    await page.evaluate(() => {
      document.addEventListener('htmx:beforeSend', event => { if (event.detail.elt.id === 'field') window.blurXhr = event.detail.xhr; });
    });
    if (outcome === 'removed') await page.locator('#field').evaluate(e => e.setAttribute('hx-swap', 'outerHTML'));
    if (outcome === 'timeout') await page.evaluate(() => {
      htmx.config.timeout = 100;
      document.addEventListener('htmx:timeout', () => { window.blurTimedOut = true; });
    });
    await startBlur(page);
    await expect.poll(() => !!blur).toBe(true);
    await page.locator('#save').click();
    if (outcome === 'changed trigger') await page.locator('#field').evaluate(e => e.setAttribute('hx-trigger', 'change'));
    if (outcome === 'abort') await page.evaluate(() => window.blurXhr.abort());
    else if (outcome === 'network error') await blur.abort();
    else if (outcome === 'timeout') await expect.poll(() => page.evaluate(() => window.blurTimedOut)).toBe(true);
    else await blur.fulfill({ status: outcome === 'HTTP error' ? 400 : 200, contentType: 'text/html', body: '<span id="replacement">Updated</span>' });
    await expect.poll(() => paths.filter(x => x === '/api/Save').length).toBe(1);
    if (outcome === 'removed') await expect(page.locator('#field')).toHaveCount(0);
  });
}

test('canceled blur requests never block a save', async ({ page }) => {
  await boot(page);
  const paths = await observeRequests(page);
  await page.evaluate(() => document.addEventListener('htmx:beforeRequest', event => {
    if (event.detail.elt.id === 'field') event.preventDefault();
  }));
  await startBlur(page);
  await page.locator('#save').click();
  await expect.poll(() => paths.filter(x => x === '/api/Save').length).toBe(1);
  expect(paths).not.toContain('/api/UpdateField');
  await expect(page.locator('#scope')).not.toHaveAttribute('aria-busy', 'true');
});

test('save times out visibly without replaying or looping', async ({ page }) => {
  let blur;
  await page.route('**/api/UpdateField', route => { blur = route; });
  await boot(page);
  const paths = await observeRequests(page);
  await page.clock.install();
  await startBlur(page);
  await expect.poll(() => !!blur).toBe(true);
  await page.locator('#save').click();
  await page.clock.runFor(1100);
  await expect(page.locator('#errors')).toContainText('Field updates are still pending. Wait for them to finish, then save again.');
  await blur.fulfill({ status: 204 });
  await page.clock.runFor(3000);
  expect(paths.filter(x => x === '/api/Save')).toHaveLength(0);
  await page.locator('#save').click();
  await expect.poll(() => paths.filter(x => x === '/api/Save').length).toBe(1);
});

test('disconnected deferred save is discarded', async ({ page }) => {
  let blur;
  await page.route('**/api/UpdateField', route => { blur = route; });
  await boot(page);
  const paths = await observeRequests(page);
  await startBlur(page);
  await expect.poll(() => !!blur).toBe(true);
  await page.locator('#save').click();
  await page.locator('#save').evaluate(e => e.remove());
  await blur.fulfill({ status: 204 });
  await page.waitForTimeout(100);
  expect(paths.filter(x => x === '/api/Save')).toHaveLength(0);
});

test('action matching ignores queries and does not match action prefixes', async ({ page }) => {
  let blur;
  await page.route(/\/api\/UpdateField\?/, route => { blur = route; });
  await boot(page);
  const paths = await observeRequests(page);
  await page.evaluate(() => {
    const field = document.getElementById('field');
    field.setAttribute('hx-post', '/api/UpdateField?next=/Save');
    const save = document.getElementById('save');
    save.setAttribute('hx-post', '/api/UpdatePreferences?next=/Save');
    htmx.process(document.body);
  });
  await startBlur(page);
  await expect.poll(() => !!blur).toBe(true);
  await page.locator('#save').click();
  await expect.poll(() => paths.includes('/api/UpdatePreferences')).toBe(true);
  await blur.fulfill({ status: 204 });
});

test('structured errors are text-only and preserve approved metadata', async ({ page }) => {
  await page.route('**/api/content', route => route.fulfill({ status: 400, contentType: 'text/html', body: `
    <div data-hc-error-fragment data-hc-error-kind="validation" data-hc-component-id="table-1" onclick="window.pwned=1">
      <strong data-hc-error-title>Invalid <em>entry</em></strong>
      <span data-hc-error-message>Try again<img src="/bad" onerror="window.pwned=1"></span>
      <script>window.pwned=1</script><iframe srcdoc="<script>parent.pwned=1</script>"></iframe>
    </div>` }));
  await boot(page);
  await page.locator('#request').click();
  await expect(page.locator('#errors')).toContainText('Invalid entry Try again');
  await expect(page.locator('#errors [data-hc-error-fragment]')).toHaveAttribute('data-hc-component-id', 'table-1');
  await expect(page.locator('#errors [data-hc-error-fragment]')).toHaveAttribute('data-hc-error-kind', 'validation');
  expect(await page.locator('#errors').evaluate(e => e.querySelectorAll('script, iframe, img, em, [onclick]').length)).toBe(0);
  expect(await page.evaluate(() => window.pwned)).toBeUndefined();
});

for (const body of ['<div data-hc-error-fragment>Broken</div>', 'Secret exception detail']) {
  test(`malformed error falls back safely: ${body.slice(0, 20)}`, async ({ page }) => {
    await page.route('**/api/content', route => route.fulfill({ status: 500, body }));
    await boot(page);
    await page.locator('#request').click();
    await expect(page.locator('#errors')).toContainText('Something went wrong');
    await expect(page.locator('#errors')).not.toContainText('Secret');
  });
}

for (const selector of ['[', 'closest [', 'find [']) {
  test(`invalid selector restores pending UI: ${selector}`, async ({ page }) => {
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await boot(page);
    await page.locator('#scope').evaluate((e, selector) => {
      e.dataset.hcPendingDisable = 'scope'; e.dataset.hcPendingDisableSelector = '[';
      e.dataset.hcStaleRegionSelector = selector; e.dataset.hcRequestIndicatorSelector = selector;
    }, selector);
    await page.locator('#request').click();
    await expect(page.locator('#loaded')).toHaveText('Loaded');
    await expect(page.locator('#scope')).not.toHaveAttribute('aria-busy', 'true');
    await expect(page.locator('#request')).not.toHaveAttribute('aria-disabled', 'true');
    expect(errors).toEqual([]);
  });
}

test('unexpected pending initialization failure rolls back partial state', async ({ page }) => {
  await boot(page);
  await page.locator('#scope').evaluate(e => {
    e.dataset.hcPendingDisable = 'scope';
    e.querySelectorAll = () => { throw new Error('Unexpected query failure'); };
  });
  const failure = page.waitForEvent('pageerror');
  await page.locator('#request').click();
  expect((await failure).message).toContain('Unexpected query failure');
  await expect(page.locator('#scope')).not.toHaveAttribute('aria-busy', 'true');
});

test('requests use the shared page-state header, including after replacement', async ({ page }) => {
  const states = [];
  await page.route('**/api/content', route => {
    states.push(route.request().headers()['x-page-state']);
    return route.fulfill({ status: 200, body: '<span>Loaded</span>' });
  });
  await boot(page);
  await page.locator('#request').click();
  await expect.poll(() => states.length).toBe(1);
  await page.locator('input[name=page_state]').evaluate(e => { e.outerHTML = '<input type="hidden" name="page_state" value="updated-state">'; });
  await page.locator('#request').click();
  await expect.poll(() => states.length).toBe(2);
  expect(states).toEqual(['shared-state', 'updated-state']);
});

for (const fallback of [false, true]) {
  for (const close of ['button', 'Escape']) {
    test(`modal ${fallback ? 'fallback' : 'native'} closes by ${close} and restores focus`, async ({ page }) => {
      await boot(page);
      if (fallback) await page.locator('#modal-one').evaluate(e => { e.showModal = undefined; e.close = undefined; });
      await page.locator('#open-one').click();
      await expect(page.locator('#modal-one')).toHaveAttribute('open', '');
      await expect(page.locator('#body-one')).toContainText('Modal content');
      if (close === 'button') await page.locator('#modal-one [data-hc-modal-close]').click();
      else await page.keyboard.press('Escape');
      await expect(page.locator('#modal-one')).not.toHaveAttribute('open');
      await expect(page.locator('#body-one')).toBeEmpty();
      await expect(page.locator('#open-one')).toBeFocused();
    });
  }
}

test('switching modals cleans up the old body without restoring its opener focus', async ({ page }) => {
  await boot(page);
  await page.locator('#open-one').click();
  await expect(page.locator('#modal-one')).toHaveAttribute('open', '');
  // A native modal makes the other opener inert, so trigger the second HTMX request programmatically.
  await page.locator('#open-two').evaluate(e => e.click());
  await expect(page.locator('#modal-two')).toHaveAttribute('open', '');
  await expect(page.locator('#modal-one')).not.toHaveAttribute('open');
  await expect(page.locator('#body-one')).toBeEmpty();
  await expect(page.locator('#open-one')).not.toBeFocused();
});

test('a response swapped outside the configured modal body does not open a modal', async ({ page }) => {
  await boot(page);
  await page.locator('#open-one').evaluate(e => { e.setAttribute('hx-target', '#target'); htmx.process(e); });
  await page.locator('#open-one').click();
  await expect(page.locator('#target')).toContainText('Modal content');
  await expect(page.locator('#modal-one')).not.toHaveAttribute('open');
});


test('existing request lifecycle smoke scenarios still pass', async ({ page }) => {
  await page.goto('/smoke');
  await expect(page.locator('#smoke-result')).toContainText('"passed": true');
});

test('clicking Save blurs the focused field before saving', async ({ page }) => {
  let blur;
  await page.route('**/api/UpdateField', route => { blur = route; });
  await boot(page);
  const paths = await observeRequests(page);
  await page.locator('#field').fill('updated');
  await page.locator('#save').click();
  await expect.poll(() => !!blur).toBe(true);
  expect(paths.filter(x => x === '/api/Save')).toHaveLength(0);
  await blur.fulfill({ status: 204 });
  await expect.poll(() => paths.filter(x => x === '/api/Save').length).toBe(1);
});


test('duplicate bundle can add a behavior and reuse the existing error reporter', async ({ page }) => {
  await boot(page, '?behaviors=error-handling');
  await page.evaluate(() => {
    document.getElementById('htmx-components-runtime-config').textContent = JSON.stringify({ behaviors: ['authentication-retry'] });
  });
  await page.addScriptTag({ url: '/runtime.js?additional-behavior' });
  await page.evaluate(() => { window.open = () => null; });
  await page.route('**/api/content', route => route.fulfill({ status: 401, headers: { 'X-Auth-Failure': 'popup-login:/popup' } }));
  await page.locator('#request').click();
  await expect(page.locator('#errors')).toContainText('popup was blocked');
  expect(await page.evaluate(() => window.bodyLoads)).toBe(1);
});

test('table synchronization includes ancestor, root, and descendant matches', async ({ page }) => {
  await boot(page);
  await page.locator('#table').evaluate(table => {
    const toggle = table.querySelector('[data-hc-table-edit-toggle]');
    toggle.classList.remove('editing-mode');
    window.HtmxComponents.init(toggle);
  });
  await expect(page.locator('#table')).not.toHaveClass('editing-mode');
  await page.locator('#table').evaluate(table => {
    table.querySelector('[data-hc-table-edit-toggle]').classList.add('editing-mode');
    window.HtmxComponents.init(table);
  });
  await expect(page.locator('#table')).toHaveClass('editing-mode');
  await page.locator('#table').evaluate(table => {
    table.querySelector('[data-hc-table-edit-toggle]').classList.remove('editing-mode');
    window.HtmxComponents.init(document.body);
  });
  await expect(page.locator('#table')).not.toHaveClass('editing-mode');
});
