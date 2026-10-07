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

    await expect(page.getByRole('link', { name: 'Iniciar sesión' })).toBeVisible();
  });

  test('login is refused until the email is confirmed', async ({ page }) => {
    const email = newEmail();
    await register(page, email);

    await login(page, email);

    await expect(page.locator('.alert.error')).toContainText('Todavía no has confirmado tu email');
    await expect(page).toHaveURL(/\/login/);
  });

  test('an unconfirmed user can ask for a new confirmation link from the login page', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await login(page, email);

    await page.getByRole('link', { name: 'Enviarme otro enlace de confirmación' }).click();
    await expect(page.getByLabel('Email')).toHaveValue(email);
    await page.getByRole('button', { name: 'Enviar enlace de confirmación' }).click();
    await expect(page.locator('.alert.success')).toBeVisible();

    await expect.poll(() => linkCount(email, 'confirmation')).toBe(2);
    await confirm(page, email);
    await login(page, email);
    await expect(page).toHaveURL(/\/collections$/);
  });

  test('a wrong password is rejected with a generic message', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await confirm(page, email);

    await login(page, email, 'Wrong-Password-9');

    await expect(page.locator('.alert.error')).toContainText('Email o contraseña incorrectos');
  });

  test('registering an existing email looks exactly like a new sign-up', async ({ page }) => {
    const email = newEmail();
    await register(page, email);
    await register(page, email);

    // Same neutral message both times (checked inside register) and no second account.
    // Emails are sent in the background, so the log is polled rather than read once.
    await expect.poll(() => linkCount(email, 'confirmation')).toBe(1);
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
    await expect(page.locator('.alert.error')).toContainText('Email o contraseña incorrectos');

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
    await expect(page.locator('.alert.error')).toContainText('no es válido o ha caducado');

    await login(page, email, 'Reset-Password-3');
    await expect(page).toHaveURL(/\/collections$/);
  });
});
