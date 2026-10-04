import fs from 'node:fs';
import { expect } from '@playwright/test';
import { apiLogFile } from './paths';

// In Development the API has no SMTP: ConsoleEmailSender logs every email link.
// This reads them back, which plays the role of the user's inbox.
type Kind = 'confirmation' | 'reset';

const patterns: Record<Kind, (email: string) => RegExp> = {
  confirmation: email => new RegExp(`Email confirmation for ${escape(email)}: (\\S+)`, 'g'),
  reset: email => new RegExp(`Password reset for ${escape(email)}: (\\S+)`, 'g')
};

export async function latestLink(email: string, kind: Kind): Promise<string> {
  let link: string | undefined;

  await expect
    .poll(() => {
      const matches = [...fs.readFileSync(apiLogFile, 'utf8').matchAll(patterns[kind](email))];
      link = matches.at(-1)?.[1];
      return link;
    }, { message: `${kind} email for ${email}`, timeout: 10_000 })
    .toBeTruthy();

  // Same path and token, served by the dev server the tests are using.
  const url = new URL(link!);
  return `${url.pathname}${url.search}`;
}

export function linkCount(email: string, kind: Kind): number {
  return [...fs.readFileSync(apiLogFile, 'utf8').matchAll(patterns[kind](email))].length;
}

function escape(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
