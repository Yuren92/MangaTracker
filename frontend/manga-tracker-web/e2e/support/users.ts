import { expect, Page } from '@playwright/test';
import { latestLink } from './mailbox';

export const password = 'Correct-Horse-1';

export function newEmail(): string {
  return `e2e-${Date.now()}-${Math.floor(Math.random() * 1e6)}@manga-tracker.test`;
}

export async function register(page: Page, email: string, pwd = password): Promise<void> {
  await page.goto('/register');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Contraseña').fill(pwd);
  await page.getByRole('button', { name: /crear cuenta/i }).click();
  await expect(page.getByText('Si el email se puede usar')).toBeVisible();
}

export async function confirm(page: Page, email: string): Promise<void> {
  await page.goto(await latestLink(email, 'confirmation'));
  await expect(page.getByText(/confirmed successfully/i)).toBeVisible();
}

export async function login(page: Page, email: string, pwd = password): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Contraseña').fill(pwd);
  await page.getByRole('button', { name: /entrar|login|iniciar/i }).click();
}

export async function signUpAndLogIn(page: Page): Promise<string> {
  const email = newEmail();
  await register(page, email);
  await confirm(page, email);
  await login(page, email);
  await expect(page).toHaveURL(/\/collections$/);
  return email;
}
