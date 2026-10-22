import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const canonical = 'https://gorangn.github.io/EasyBluetoothAudio/';
const download = 'https://github.com/GorangN/EasyBluetoothAudio/releases/latest/download/EasyBluetoothAudioSetup.exe';

test('publishes complete metadata and resolves assets below the repository base path', async ({ page, request }) => {
  const failedRequests: string[] = [];
  const requestedAssets: string[] = [];
  page.on('requestfailed', (failed) => failedRequests.push(`${failed.url()}: ${failed.failure()?.errorText}`));
  page.on('response', (response) => {
    if (response.status() >= 400) {
      failedRequests.push(`${response.status()} ${response.url()}`);
    }
    if (['stylesheet', 'script', 'image', 'font'].includes(response.request().resourceType())) {
      requestedAssets.push(response.url());
    }
  });

  await page.goto('./');
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveTitle('EasyBluetoothAudio | Free Bluetooth Audio for Windows');
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', canonical);
  await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', /free, open-source Bluetooth audio receiver/i);
  await expect(page.locator('meta[property="og:url"]')).toHaveAttribute('content', canonical);
  await expect(page.locator('meta[name="twitter:card"]')).toHaveAttribute('content', 'summary_large_image');

  const faviconPath = await page.locator('link[rel="icon"]').getAttribute('href');
  const socialImage = await page.locator('meta[property="og:image"]').getAttribute('content');
  expect(faviconPath).toBe('/EasyBluetoothAudio/favicon.svg');
  expect(socialImage).toBe(`${canonical}social-preview.png`);
  for (const assetPath of [faviconPath!, new URL(socialImage!).pathname]) {
    const response = await request.get(assetPath);
    expect(response.ok(), assetPath).toBeTruthy();
    expect(response.headers()['content-type']).toMatch(/image\//);
  }

  expect(requestedAssets.length).toBeGreaterThan(0);
  for (const assetUrl of requestedAssets) {
    const asset = new URL(assetUrl);
    expect(asset.origin).toBe(new URL(page.url()).origin);
    expect(asset.pathname).toMatch(/^\/EasyBluetoothAudio\//);
  }
  expect(failedRequests).toEqual([]);
});

test('keeps one download destination and supports keyboard navigation and native FAQs', async ({ page }) => {
  await page.goto('./');
  const downloadLinks = page.getByRole('link', { name: /download (for windows|free)/i });
  expect(await downloadLinks.count()).toBeGreaterThanOrEqual(3);
  for (const downloadLink of await downloadLinks.all()) {
    await expect(downloadLink).toHaveAttribute('href', download);
  }
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Skip to content' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/#main$/);

  const question = page.locator('details').filter({ hasText: 'Free? What’s the catch?' });
  const summary = question.locator('summary');
  await summary.focus();
  await page.keyboard.press('Enter');
  await expect(question).toHaveAttribute('open', '');
  await expect(question.locator('p')).toBeVisible();
  await page.keyboard.press('Space');
  await expect(question).not.toHaveAttribute('open');
});

test('hydrates the audio preview, switches output, and pauses motion without console errors', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  page.on('console', (message) => {
    if (message.type() === 'error') {
      errors.push(message.text());
    }
  });
  await page.goto('./');
  const preview = page.locator('.audio-preview');
  await preview.scrollIntoViewIfNeeded();
  const headphones = page.getByRole('button', { name: 'Headphones', exact: true });
  const speakers = page.getByRole('button', { name: 'Speakers', exact: true });
  await headphones.click();
  await expect(headphones).toHaveAttribute('aria-pressed', 'true');
  await expect(speakers).toHaveAttribute('aria-pressed', 'false');
  await expect(preview.locator('[aria-live="polite"]')).toHaveText('Preview output: headphones');
  await speakers.click();
  await expect(speakers).toHaveAttribute('aria-pressed', 'true');

  await expect(preview).toHaveAttribute('data-motion', 'paused');
  await page.getByRole('button', { name: 'Play preview animation' }).click();
  await expect(preview).toHaveAttribute('data-motion', 'playing');
  await page.getByRole('button', { name: 'Pause preview animation' }).click();
  const resume = page.getByRole('button', { name: 'Play preview animation' });
  await expect(resume).toHaveAttribute('aria-pressed', 'true');
  await expect(preview.locator('.signal-bars i').first()).toHaveCSS('animation-play-state', 'paused');
  const pausedGradients = await preview.locator('linearGradient').evaluateAll((gradients) => gradients.map((gradient) => gradient.outerHTML));
  await page.waitForTimeout(150);
  expect(await preview.locator('linearGradient').evaluateAll((gradients) => gradients.map((gradient) => gradient.outerHTML))).toEqual(pausedGradients);
  await resume.click();
  await expect(page.getByRole('button', { name: 'Pause preview animation' })).toHaveAttribute('aria-pressed', 'false');
  await expect(preview).toHaveAttribute('data-motion', 'paused', { timeout: 7000 });
  expect(errors).toEqual([]);
});

test('prevents lost preview clicks while its JavaScript is still loading', async ({ page }) => {
  let releaseScripts!: () => void;
  const scriptGate = new Promise<void>((resolve) => { releaseScripts = resolve; });
  await page.route('**/_astro/*.js', async (route) => {
    await scriptGate;
    await route.continue();
  });
  const headphones = page.getByRole('button', { name: 'Headphones', exact: true });
  try {
    await page.goto('./', { waitUntil: 'commit' });
    await expect(headphones).toBeDisabled();
    await expect(page.locator('.hero').getByRole('link', { name: 'Download for Windows' })).toBeEnabled();
  } finally {
    releaseScripts();
  }
  await headphones.click();
  await expect(headphones).toHaveAttribute('aria-pressed', 'true');
});

for (const width of [320, 390, 720, 768, 1440]) {
  test(`fits a ${width}px viewport without horizontal overflow`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('./');
    await page.evaluate(() => document.fonts.ready);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expect(page.locator('.hero').getByRole('link', { name: 'Download for Windows' })).toBeVisible();
    await expect(page.locator('.site-footer')).toBeVisible();
    if (width <= 800) {
      await expect(page.locator('.phone-beam')).toBeHidden();
    } else {
      await expect(page.locator('.phone-beam')).toBeVisible();
    }
    const sizes = await page.evaluate(() => ({
      viewport: document.documentElement.clientWidth,
      document: document.documentElement.scrollWidth,
      body: document.body.scrollWidth,
    }));
    expect(sizes.document).toBeLessThanOrEqual(sizes.viewport);
    expect(sizes.body).toBeLessThanOrEqual(sizes.viewport);
    for (const control of await page.locator('a:not(.skip-link), button, summary').all()) {
      if (await control.isVisible()) {
        const bounds = await control.boundingBox();
        expect(bounds!.height, await control.innerText()).toBeGreaterThanOrEqual(44);
        expect(bounds!.width, await control.innerText()).toBeGreaterThanOrEqual(44);
      }
    }
  });
}

for (const width of [390, 1440]) {
  test(`has no axe accessibility violations at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 1000 });
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.goto('./');
    await page.locator('details summary').first().click();
    const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'best-practice']).analyze();
    expect(results.violations).toEqual([]);
  });
}

test('honors reduced motion for CSS effects and animated beams', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('./');
  await page.locator('.audio-preview').scrollIntoViewIfNeeded();
  await expect(page.locator('.signal-bars i').first()).toHaveCSS('animation-name', 'none');
  await expect(page.locator('.magic-shimmer-slide').first()).toHaveCSS('animation-name', 'none');
  await page.locator('.magic-shimmer-button').first().hover();
  await expect(page.locator('.magic-shimmer-slide').first()).toHaveCSS('animation-name', 'none');
  await expect(page.locator('html')).toHaveCSS('scroll-behavior', 'auto');
  await expect(page.locator('.magic-beam').first()).toHaveAttribute('width', /^[1-9]/);
  const gradients = page.locator('.magic-beam linearGradient');
  const initial = await gradients.evaluateAll((elements) => elements.map((element) => element.outerHTML));
  await page.waitForTimeout(150);
  expect(await gradients.evaluateAll((elements) => elements.map((element) => element.outerHTML))).toEqual(initial);
});

test('provides real section navigation, every FAQ answer, and bounded button motion', async ({ page }) => {
  await page.goto('./');
  await page.getByRole('link', { name: 'How it works', exact: true }).click();
  await expect(page).toHaveURL(/#how-it-works$/);
  await page.getByRole('link', { name: 'Questions', exact: true }).click();
  await expect(page).toHaveURL(/#questions$/);
  for (const details of await page.locator('details').all()) {
    await details.locator('summary').click();
    await expect(details).toHaveAttribute('open', '');
    await expect(details.locator('p')).toBeVisible();
    await details.locator('summary').click();
    await expect(details).not.toHaveAttribute('open');
  }
  const downloadButton = page.locator('.magic-shimmer-button').first();
  await downloadButton.hover();
  await expect(downloadButton.locator('.magic-shimmer-slide')).toHaveCSS('animation-iteration-count', '2');
  await expect(downloadButton.locator('.magic-shimmer-spark')).toHaveCSS('animation-iteration-count', '1');
  await downloadButton.focus();
  await page.keyboard.press('Tab');
  await page.keyboard.press('Shift+Tab');
  await expect(downloadButton).toHaveCSS('outline-style', 'solid');
  await expect(downloadButton).toHaveCSS('outline-width', '3px');
  expect(await page.locator('body').innerText()).not.toContain('\u2014');
});

test.describe('without JavaScript', () => {
  test.use({ javaScriptEnabled: false });

  test('retains the product pitch, download links, and native FAQ answers', async ({ page }) => {
    await page.goto('./');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Your phone.Your PC speakers.');
    const heroDownload = page.locator('.hero').getByRole('link', { name: 'Download for Windows' });
    await expect(heroDownload).toBeVisible();
    await expect(heroDownload).toHaveAttribute('href', download);
    await expect(page.locator('.motion-toggle')).toBeHidden();
    await expect(page.locator('.output-switch')).toBeHidden();
    await expect(page.locator('.signal-bars i').first()).toHaveCSS('animation-name', 'none');
    await expect(page.getByRole('heading', { name: /Pair once\.\s*Press play\./ })).toBeVisible();
    const question = page.locator('details').filter({ hasText: 'Will it work on my PC?' });
    await question.locator('summary').click();
    await expect(question.locator('p')).toBeVisible();
    await expect(question.locator('p')).toContainText('Windows 10 version 2004');
  });
});
