import { expect, test } from '@playwright/test';
import { latestLink, linkCount } from './support/mailbox';
import { confirm, login, newEmail, password, register, signUpAndLogIn } from './support/users';

test.describe('Authentication', () => {
  test('sign up, confirm the email, log in and log out', async ({ page }) => {
    const email = await signUpAndLogIn(page);

    await expect(page.getByRole('heading', { name: 'Mis colecciones' })).toBeVisible();

    await page.locator('.user-menu-button').click();
    await expect(page.locator('.user-menu')).toContainText(email);
    await page.getByRole('button', { name: 'Cerrar sesión' }).click();

    await expect(page.getByRole('link', { name: 'Login' })).toBeVisible();
  });

  test('login is refused until the email is confirmed', async ({ page }) => {
    const email = newEmail();
    await register(page, email);

    await login(page, email);

    await expect(page.locator('.alert.error')).toContainText('Email is not confirmed');
    await expect(page).toHaveURL(/\/login/);
  });

  test('a wrong password is rejected with a generic message', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await confirm(page, email);

    await login(page, email, 'Wrong-Password-9');

    await expect(page.locator('.alert.error')).toContainText('Invalid email or password');
  });

  test('registering an existing email looks exactly like a new sign-up', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await register(page, email);

    // Same neutral message both times (checked inside register) and no second account.
    expect(linkCount(email, 'confirmation')).toBe(1);
  });

  test('protected pages send guests to login and come back after signing in', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await confirm(page, email);

    await page.goto('/collections/pending-tomes');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);

    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Contraseña').fill(password);
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page).toHaveURL(/\/collections\/pending-tomes$/);
  });

  test('changing the password keeps this session and replaces the old password', async ({ page }) => {
    const email = await signUpAndLogIn(page);
    const newPassword = 'Changed-Password-2';

    await page.goto('/change-password');
    await page.getByLabel('Contraseña actual').fill(password);
    await page.getByLabel('Nueva contraseña').fill(newPassword);
    await page.getByRole('button', { name: 'Cambiar contraseña' }).click();
    await expect(page.locator('.alert.success')).toBeVisible();

    // The session survives because the response carried a fresh token.
    await page.getByRole('link', { name: 'Mis colecciones', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Mis colecciones' })).toBeVisible();

    await page.locator('.user-menu-button').click();
    await page.getByRole('button', { name: 'Cerrar sesión' }).click();

    await login(page, email, password);
    await expect(page.locator('.alert.error')).toContainText('Invalid email or password');

    await login(page, email, newPassword);
    await expect(page).toHaveURL(/\/collections$/);
  });

  test('a password reset link works once', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await confirm(page, email);

    await page.goto('/forgot-password');
    await page.getByLabel('Email').fill(email);
    await page.getByRole('button', { name: 'Enviar enlace' }).click();
    await expect(page.locator('.alert.success')).toBeVisible();

    const resetLink = await latestLink(email, 'reset');

    await page.goto(resetLink);
    await page.getByLabel('Nueva contraseña').fill('Reset-Password-3');
    await page.getByRole('button', { name: 'Cambiar contraseña' }).click();
    await expect(page.locator('.alert.success')).toBeVisible();

    // The same link cannot be used again.
    await page.goto(resetLink);
    await page.getByLabel('Nueva contraseña').fill('Another-Password-4');
    await page.getByRole('button', { name: 'Cambiar contraseña' }).click();
    await expect(page.locator('.alert.error')).toContainText(/invalid or expired/i);

    await login(page, email, 'Reset-Password-3');
    await expect(page).toHaveURL(/\/collections$/);
  });
});
