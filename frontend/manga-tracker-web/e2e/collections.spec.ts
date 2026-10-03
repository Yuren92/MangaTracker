import { expect, test } from '@playwright/test';
import { signUpAndLogIn } from './support/users';

// Uses the real Comic Vine API through the backend, so it needs a valid
// ComicVine:ApiKey in appsettings.Development.json. Without it the backend answers
// 503 and the test is skipped. Each run spends a few requests of the Comic Vine
// hourly quota, so it picks a volume with few issues.
const searchTerm = 'Berserk';
const maxIssues = 12;

test('search, import a volume, track owned tomes and delete the collection', async ({ page }) => {
  await signUpAndLogIn(page);

  await page.goto('/catalog/search');
  await page.getByPlaceholder(/One Piece/).fill(searchTerm);

  const searchResponse = page.waitForResponse(response => response.url().includes('/api/catalog/search'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  const search = await searchResponse;
  test.skip(search.status() === 503, 'Comic Vine is not reachable (is ComicVine:ApiKey set?)');
  expect(search.status()).toBe(200);

  // Pick the first result small enough to import quickly.
  const volumes = (await search.json()) as { name: string; countOfIssues: number | null }[];
  const index = volumes.findIndex(volume => (volume.countOfIssues ?? 0) > 0 && volume.countOfIssues! <= maxIssues);
  test.skip(index < 0, `No "${searchTerm}" volume with 1..${maxIssues} issues in the results`);

  await page.getByRole('button', { name: 'Ver edición' }).nth(index).click();
  await page.getByRole('button', { name: 'Importar esta edición' }).click();
  await expect(page.locator('.alert.success')).toBeVisible({ timeout: 45_000 });
  await page.getByRole('link', { name: 'Ver en mi colección' }).click();

  // Detail: nothing owned yet, then mark one tome.
  await expect(page.locator('.stats')).toContainText('0 comprados');
  const total = Number((await page.locator('.stats span').first().innerText()).match(/\d+/)![0]);
  expect(total).toBeGreaterThan(0);

  await page.locator('button.owned-state').first().click();
  await expect(page.locator('button.owned-state').first()).toHaveText(/Comprado/);
  await expect(page.locator('.stats')).toContainText('1 comprados');

  // Pending: everything except the tome just marked.
  await page.getByRole('link', { name: 'Pendientes', exact: true }).click();
  const markButtons = page.getByRole('button', { name: 'Marcar comprado' });
  await expect(markButtons).toHaveCount(total - 1);

  if (total > 1) {
    await markButtons.first().click();
    await expect(markButtons).toHaveCount(total - 2);
  }

  // Collections list, then delete (the page asks for confirmation).
  await page.getByRole('link', { name: 'Mis colecciones', exact: true }).click();
  await expect(page.locator('article.collection-card')).toHaveCount(1);

  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: /eliminar/i }).first().click();
  await expect(page.getByText('Aún no tienes colecciones')).toBeVisible();
});
