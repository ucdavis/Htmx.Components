import { test, expect } from '@playwright/test';

async function boot(page, query = '') {
  await page.goto(`/fixture${query}`);
  await expect.poll(() => page.evaluate(() => window.bodyLoads)).toBe(1);
  await page.evaluate(() => {
    const open = window.open.bind(window);
    window.open = (...args) => (window.loginPopup = open(...args));
  });
}
async function challenge(page) {
  let requests = 0;
  await page.route('**/api/content', route => {
    requests++;
    return route.fulfill(requests === 1
      ? { status: 401, headers: { 'X-Auth-Failure': 'popup-login:/popup' } }
      : { status: 200, body: '<span id="authenticated">Authenticated</span>' });
  });
  return () => requests;
}
async function openPopup(page) {
  const popupEvent = page.waitForEvent('popup');
  await page.locator('#request').click();
  const popup = await popupEvent;
  await popup.waitForLoadState();
  return popup;
}

test('only the expected popup and origin can replay the request', async ({ page }) => {
  await boot(page);
  const requests = await challenge(page);
  const popup = await openPopup(page);
  await page.evaluate(() => {
    window.dispatchEvent(new MessageEvent('message', { data: 'login-success', origin: 'https://unrelated.invalid', source: window.loginPopup }));
    window.dispatchEvent(new MessageEvent('message', { data: 'login-success', origin: location.origin, source: window }));
  });
  await page.waitForTimeout(100);
  expect(requests()).toBe(1);
  await popup.evaluate(() => window.opener.postMessage('login-success', location.origin));
  await expect(page.locator('#authenticated')).toHaveText('Authenticated');
  await popup.evaluate(() => window.opener.postMessage('login-success', location.origin));
  await page.waitForTimeout(50);
  expect(requests()).toBe(2);
});

test('only one popup is opened for concurrent authentication failures', async ({ page }) => {
  await boot(page);
  await page.route('**/api/**', route => route.fulfill({ status: 401, headers: { 'X-Auth-Failure': 'popup-login:/popup' } }));
  const popup = await openPopup(page);
  let extraPopups = 0;
  page.on('popup', () => extraPopups++);
  await page.locator('#save').click();
  await page.waitForTimeout(100);
  expect(extraPopups).toBe(0);
  await popup.close();
  await expect(page.locator('#errors')).toContainText('popup was closed');
});

for (const excluded of [false, true]) {
  test(`blocked popup returns immediately and releases guard${excluded ? ' without error handling' : ''}`, async ({ page }) => {
    const warnings = [];
    page.on('console', message => { if (message.type() === 'warning') warnings.push(message.text()); });
    await boot(page, excluded ? '?behaviors=authentication-retry' : '');
    await page.route('**/api/content', route => route.fulfill({ status: 401, headers: { 'X-Auth-Failure': 'popup-login:/popup' } }));
    await page.evaluate(() => { window.openCount = 0; window.open = () => { window.openCount++; return null; }; });
    await page.locator('#request').click();
    if (excluded) await expect.poll(() => warnings.some(x => x.includes('popup was blocked'))).toBe(true);
    else await expect(page.locator('#errors')).toContainText('popup was blocked');
    await page.locator('#request').click();
    await expect.poll(() => page.evaluate(() => window.openCount)).toBe(2);
  });
}

for (const outcome of ['closed', 'timeout']) {
  test(`login ${outcome} reports failure and permits another attempt`, async ({ page }) => {
    await boot(page);
    await page.route('**/api/content', route => route.fulfill({ status: 401, headers: { 'X-Auth-Failure': 'popup-login:/popup' } }));
    await page.clock.install();
    const popup = await openPopup(page);
    if (outcome === 'closed') await popup.close();
    await page.clock.runFor(outcome === 'closed' ? 300 : 31000);
    await expect(page.locator('#errors')).toContainText(outcome === 'closed' ? 'popup was closed' : 'Sign-in timed out');
    await popup.close();
    const second = await openPopup(page);
    await second.close();
  });
}

test('authentication replay preserves a real Shift-click through its trigger filter', async ({ page }) => {
  await boot(page);
  const requests = await challenge(page);
  await page.locator('#request').evaluate(element => {
    element.setAttribute('hx-trigger', 'click[shiftKey]');
    htmx.process(element);
    window.replayedClicks = [];
    element.addEventListener('click', event => window.replayedClicks.push({
      name: event.constructor.name, shiftKey: event.shiftKey, isTrusted: event.isTrusted,
    }));
  });
  const popupEvent = page.waitForEvent('popup');
  await page.locator('#request').click({ modifiers: ['Shift'] });
  const popup = await popupEvent;
  await popup.waitForLoadState();
  expect(await page.evaluate(() => window.replayedClicks)).toEqual([
    { name: 'PointerEvent', shiftKey: true, isTrusted: true },
  ]);
  expect(requests()).toBe(1);
  await popup.evaluate(() => window.opener.postMessage('login-success', location.origin));
  await expect(page.locator('#authenticated')).toHaveText('Authenticated');
  expect(requests()).toBe(2);
  expect(await page.evaluate(() => window.replayedClicks)).toEqual([
    { name: 'PointerEvent', shiftKey: true, isTrusted: true },
    { name: 'MouseEvent', shiftKey: true, isTrusted: false },
  ]);
});

for (const eventClass of ['MouseEvent', 'PointerEvent', 'WheelEvent', 'KeyboardEvent', 'CustomEvent', 'unsupported']) {
  test(`authentication replay preserves supported event data: ${eventClass}`, async ({ page }) => {
    await boot(page);
    const requests = await challenge(page);
    await page.locator('#request').evaluate(element => {
      element.setAttribute('hx-trigger', 'retry');
      htmx.process(element);
      window.replayedEvents = [];
      element.addEventListener('retry', event => window.replayedEvents.push({
        name: event.constructor.name, detail: event.detail, key: event.key, ctrlKey: event.ctrlKey,
        shiftKey: event.shiftKey, clientX: event.clientX, clientY: event.clientY,
        screenX: event.screenX, screenY: event.screenY,
      }));
    });
    const popupEvent = page.waitForEvent('popup');
    await page.locator('#request').evaluate((element, name) => {
      // Its constructor name resolves to a non-constructor global in the old implementation.
      class location extends Event { constructor() { super('retry', { bubbles: true }); } }
      const mouseInit = { bubbles: true, ctrlKey: true, shiftKey: true, clientX: 12, clientY: 34, screenX: 56, screenY: 78 };
      const event = name === 'unsupported' ? new location()
        : name === 'MouseEvent' ? new MouseEvent('retry', mouseInit)
        : name === 'PointerEvent' ? new PointerEvent('retry', mouseInit)
        : name === 'WheelEvent' ? new WheelEvent('retry', mouseInit)
        : name === 'KeyboardEvent' ? new KeyboardEvent('retry', { bubbles: true, key: 'Enter' })
        : new CustomEvent('retry', { bubbles: true, detail: { token: 42 } });
      element.dispatchEvent(event);
    }, eventClass);
    const popup = await popupEvent;
    await popup.waitForLoadState();
    await popup.evaluate(() => window.opener.postMessage('login-success', location.origin));
    await expect(page.locator('#authenticated')).toHaveText('Authenticated');
    expect(requests()).toBe(2);
    const replay = await page.evaluate(() => window.replayedEvents[1]);
    const mouseEvent = ['MouseEvent', 'PointerEvent', 'WheelEvent'].includes(eventClass);
    expect(replay.name).toBe(mouseEvent ? 'MouseEvent' : eventClass === 'unsupported' ? 'Event' : eventClass);
    if (mouseEvent) expect(replay).toMatchObject({ ctrlKey: true, shiftKey: true, clientX: 12, clientY: 34, screenX: 56, screenY: 78 });
    if (eventClass === 'KeyboardEvent') expect(replay.key).toBe('Enter');
    if (eventClass === 'CustomEvent') expect(replay.detail).toEqual({ token: 42 });
  });
}
