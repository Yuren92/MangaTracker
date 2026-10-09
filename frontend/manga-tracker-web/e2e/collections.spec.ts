import { expect, test } from '@playwright/test';
import { signUpAndLogIn } from './support/users';

// Uses the real Comic Vine API through the backend, so it needs a valid
// ComicVine:ApiKey in appsettings.Development.json. Without it the backend answers
// 503 and the test is skipped. Each run spends a few requests of the Comic Vine
// quota, so it picks a volume with few issues.
const searchTerm = 'Berserk';
const maxIssues = 12;

test('add a series, track owned tomes, undo, and remove it from the shelf', async ({ page }) => {
  await signUpAndLogIn(page);

  await page.goto('/catalog/search');
  await page.getByPlaceholder(/One Piece/).fill(searchTerm);

  const searchResponse = page.waitForResponse(response => response.url().includes('/api/catalog/search'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  const search = await searchResponse;
  test.skip(search.status() === 503, 'Comic Vine is not reachable (is ComicVine:ApiKey set?)');
  expect(search.status()).toBe(200);

  // Pick the first result small enough to import quickly. A new user has nothing on
  // the shelf, so every result can be added and they come in the response order.
  const volumes = (await search.json()) as { countOfIssues: number | null }[];
  // At least two tomes: one gets marked and another must still be missing.
  const index = volumes.findIndex(volume => (volume.countOfIssues ?? 0) >= 2 && volume.countOfIssues! <= maxIssues);
  test.skip(index < 0, `No "${searchTerm}" volume with 2..${maxIssues} issues in the results`);

  // Adding answers straight away; the tomes are downloaded in the background and
  // the series page fills in by itself while they arrive.
  const card = page.locator('li.edition').nth(index);
  await card.getByRole('button', { name: /^Añadir/ }).click();
  await card.getByRole('link', { name: 'Ver en mi estantería' }).click({ timeout: 45_000 });

  // The series: nothing owned yet, then one tome marked from the shelf.
  const count = page.getByTestId('series-count');
  await expect(count).toContainText(/^0 de \d+ tomos/, { timeout: 45_000 });
  const total = Number((await count.innerText()).match(/de (\d+)/)![1]);
  expect(total).toBeGreaterThan(0);

  const firstTome = page.locator('button.tome').first();
  await firstTome.click();
  await expect(firstTome).toHaveAttribute('aria-pressed', 'true');
  await expect(count).toContainText(/^1 de /);

  // "Me faltan": everything but that tome; marking one can be undone.
  await page.getByRole('link', { name: 'Me faltan', exact: true }).click();
  const ownButtons = page.getByRole('button', { name: /^Lo tengo/ });
  await expect(ownButtons).toHaveCount(total - 1);

  if (total > 1) {
    await ownButtons.first().click();
    await expect(ownButtons).toHaveCount(total - 2);

    await page.getByRole('button', { name: 'Deshacer' }).click();
    await expect(ownButtons).toHaveCount(total - 1);
  }

  // Back on the shelf, open the series and remove it (the page asks for confirmation).
  await page.getByRole('link', { name: 'Estantería', exact: true }).click();
  await expect(page.locator('a.series')).toHaveCount(1);
  await page.locator('a.series').click();

  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Quitar de la estantería' }).click();
  await expect(page).toHaveURL(/\/collections$/);
  await expect(page.getByText('Tu estantería está vacía')).toBeVisible();
});
