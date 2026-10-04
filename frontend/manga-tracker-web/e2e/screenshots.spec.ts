import path from 'node:path';
import { expect, Page, test } from '@playwright/test';
import { confirm, login, register } from './support/users';
import { repoRoot } from './support/paths';

// Not a test: regenerates the README screenshots from the real app.
// Run with: SCREENSHOTS=1 npx playwright test e2e/screenshots.spec.ts
// Uses the real Comic Vine API (about 15 requests of the hourly quota).
const out = (name: string) => path.join(repoRoot, 'docs', 'images', `${name}.png`);

// Covers come from Comic Vine and load lazily: wait until every visible image is done.
async function shoot(page: Page, name: string, fullPage = false): Promise<void> {
  await page.waitForLoadState('networkidle');
  await page.waitForFunction(() => [...document.images].every(image => image.complete));
  await page.screenshot({ path: out(name), fullPage });
}

// Typing before the page has rendered can be lost; wait for it and check the value.
async function search(page: Page, text: string): Promise<void> {
  await expect(page.getByRole('heading', { name: 'Buscar series' })).toBeVisible();
  const input = page.getByPlaceholder(/One Piece/);
  await input.fill(text);
  await expect(input).toHaveValue(text);
}

const nav = (page: Page, name: string) => page.locator('header.app-header').getByRole('link', { name, exact: true });

test.skip(!process.env.SCREENSHOTS, 'Set SCREENSHOTS=1 to regenerate the README screenshots');
test.use({ viewport: { width: 1280, height: 800 } });

test('README screenshots', async ({ page }) => {
  test.setTimeout(180_000);

  await page.goto('/login');
  await shoot(page, 'login');

  const email = `lector.${Date.now() % 100000}@mangatracker.dev`;
  await register(page, email);
  await confirm(page, email);
  await login(page, email);
  await expect(page).toHaveURL(/\/collections$/);

  await page.goto('/catalog/search');
  await search(page, 'Berserk');
  const searchResponse = page.waitForResponse(response => response.url().includes('/api/catalog/search'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  const volumes = (await (await searchResponse).json()) as { countOfIssues: number | null }[];
  await expect(page.getByRole('button', { name: 'Ver edición' }).first()).toBeVisible();
  await shoot(page, 'catalog-search');

  const index = volumes.findIndex(volume => (volume.countOfIssues ?? 0) >= 6 && volume.countOfIssues! <= 14);
  test.skip(index < 0, 'No small enough volume to import');

  await page.getByRole('button', { name: 'Ver edición' }).nth(index).click();
  await expect(page.getByRole('button', { name: 'Importar esta edición' })).toBeVisible();
  await page.locator('.preview-card').evaluate(card => card.scrollIntoView({ block: 'center' }));
  await shoot(page, 'catalog-preview');

  await page.getByRole('button', { name: 'Importar esta edición' }).click();
  await expect(page.locator('.alert.success')).toBeVisible({ timeout: 90_000 });
  await page.getByRole('link', { name: 'Ver en mi colección' }).click();

  const owned = page.locator('button.owned-state');
  for (let i = 0; i < 3; i++) {
    await owned.nth(i).click();
    await expect(owned.nth(i)).toHaveText(/Comprado/);
  }
  await shoot(page, 'collection-detail', true);

  await nav(page, 'Pendientes').click();
  await expect(page.getByRole('button', { name: 'Marcar comprado' }).first()).toBeVisible();
  await shoot(page, 'pending-tomes');

  // A second, short series so the collections page is not a single card.
  await nav(page, 'Buscar').click();
  await search(page, 'Pluto');
  const second = page.waitForResponse(response => response.url().includes('/api/catalog/search'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  const plutos = (await (await second).json()) as { countOfIssues: number | null }[];
  const plutoIndex = plutos.findIndex(volume => (volume.countOfIssues ?? 0) >= 4 && volume.countOfIssues! <= 10);
  if (plutoIndex >= 0) {
    await page.getByRole('button', { name: 'Ver edición' }).nth(plutoIndex).click();
    await page.getByRole('button', { name: 'Importar esta edición' }).click();
    await expect(page.locator('.alert.success')).toBeVisible({ timeout: 90_000 });
  }

  // The page may start a background sync on load: let its banner come and go.
  await nav(page, 'Mis colecciones').click();
  const syncing = page.getByText('Actualizando colecciones');
  await syncing.waitFor({ state: 'visible', timeout: 3_000 }).catch(() => undefined);
  await expect(syncing).toBeHidden({ timeout: 60_000 });
  await shoot(page, 'collections');
});
