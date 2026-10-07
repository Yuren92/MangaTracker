import path from 'node:path';
import { expect, Page, test } from '@playwright/test';
import { confirm, login, register } from './support/users';
import { repoRoot } from './support/paths';

// Not a test: regenerates the README screenshots from the real app.
// Run with: SCREENSHOTS=1 npx playwright test e2e/screenshots.spec.ts
// Uses the real Comic Vine API (a handful of requests).
const out = (name: string) => path.join(repoRoot, 'docs', 'images', `${name}.png`);

// Waits until the page has really settled: no loading message on screen, and every
// cover downloaded (covers load lazily, so the page is scrolled through first).
async function shoot(page: Page, name: string, fullPage = false): Promise<void> {
  await expect(page.locator('.state')).toHaveCount(0);
  await page.evaluate(async () => {
    for (let y = 0; y < document.body.scrollHeight; y += 400) {
      window.scrollTo(0, y);
      await new Promise(resolve => setTimeout(resolve, 60));
    }
    window.scrollTo(0, 0);
  });
  await page.waitForLoadState('networkidle');
  await page.waitForFunction(() => [...document.images].every(image => image.complete));
  await page.screenshot({ path: out(name), fullPage });
}

async function search(page: Page, text: string): Promise<{ countOfIssues: number | null }[]> {
  await expect(page.getByRole('heading', { name: 'Añadir serie' })).toBeVisible();
  const input = page.getByPlaceholder(/One Piece/);
  await input.fill(text);
  await expect(input).toHaveValue(text);

  const response = page.waitForResponse(candidate => candidate.url().includes('/api/catalog/search'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  const volumes = await (await response).json();
  await expect(page.locator('.edition').first()).toBeVisible();
  return volumes;
}

async function addToShelf(page: Page, index: number): Promise<void> {
  await page.locator('.edition').nth(index).click();
  await page.getByRole('button', { name: 'Añadir a mi estantería' }).click();
  await expect(page.getByRole('link', { name: 'Ver en mi estantería' })).toBeVisible({ timeout: 90_000 });
}

const nav = (page: Page, name: string) => page.locator('header.app-header').getByRole('link', { name, exact: true });

test.skip(!process.env.SCREENSHOTS, 'Set SCREENSHOTS=1 to regenerate the README screenshots');
test.use({ viewport: { width: 1280, height: 800 } });

test('README screenshots', async ({ page }) => {
  test.setTimeout(240_000);

  await page.goto('/login');
  await shoot(page, 'login');

  const email = `lector.${Date.now() % 100000}@mangatracker.dev`;
  await register(page, email);
  await confirm(page, email);
  await login(page, email);
  await expect(page).toHaveURL(/\/collections$/);

  await page.goto('/catalog/search');
  const volumes = await search(page, 'Berserk');
  await shoot(page, 'catalog-search');

  const index = volumes.findIndex(volume => (volume.countOfIssues ?? 0) >= 6 && volume.countOfIssues! <= 14);
  test.skip(index < 0, 'No small enough volume to import');

  await page.locator('.edition').nth(index).click();
  await expect(page.getByRole('button', { name: 'Añadir a mi estantería' })).toBeVisible();
  await shoot(page, 'catalog-preview');

  await page.getByRole('button', { name: 'Añadir a mi estantería' }).click();
  await page.getByRole('link', { name: 'Ver en mi estantería' }).click({ timeout: 90_000 });

  // Own the first three tomes so the shelf shows both states.
  const tomes = page.locator('button.tome');
  for (let i = 0; i < 3; i++) {
    await tomes.nth(i).click();
    await expect(tomes.nth(i)).toHaveAttribute('aria-pressed', 'true');
  }
  await shoot(page, 'collection-detail', true);

  // A second, short series so the shelf and "Me faltan" are not a single series.
  await nav(page, 'Añadir serie').click();
  const plutos = await search(page, 'Pluto');
  const plutoIndex = plutos.findIndex(volume => (volume.countOfIssues ?? 0) >= 4 && volume.countOfIssues! <= 10);
  if (plutoIndex >= 0) {
    await addToShelf(page, plutoIndex);
  }

  await page.keyboard.press('Escape');
  await nav(page, 'Me faltan').click();
  await expect(page.getByRole('button', { name: /^Lo tengo/ }).first()).toBeVisible();
  await shoot(page, 'pending-tomes', true);

  await nav(page, 'Estantería').click();
  await expect(page.locator('a.series').first()).toBeVisible();
  await shoot(page, 'collections');
});
