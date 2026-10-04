import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { apiLogFile, apiPidFile, apiUrl, artifactsDir, repoRoot } from './support/paths';

export default async function globalSetup(): Promise<void> {
  if (await isUp()) {
    throw new Error(
      `Something is already listening on ${apiUrl}. Stop it first: the tests start their own API to read its log.`);
  }

  fs.mkdirSync(artifactsDir, { recursive: true });
  const log = fs.openSync(apiLogFile, 'w');

  const api = spawn(
    'dotnet',
    ['run', '--project', path.join(repoRoot, 'src', 'MangaTracker.Api'), '--no-launch-profile'],
    {
      cwd: repoRoot,
      stdio: ['ignore', log, log],
      // Own process group on Linux/macOS so teardown can stop dotnet run and the API it starts.
      detached: process.platform !== 'win32',
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: apiUrl,
        // Many sign-ups and logins happen within a minute from the same machine.
        'RateLimiting__auth-sensitive': '1000',
        'RateLimiting__external-api': '1000',
        'RateLimiting__comic-vine-import': '1000'
      }
    });

  fs.writeFileSync(apiPidFile, String(api.pid));

  const deadline = Date.now() + 180_000;
  while (Date.now() < deadline) {
    if (await isUp()) {
      return;
    }
    if (api.exitCode !== null) {
      throw new Error(`The API exited during startup. See ${apiLogFile}`);
    }
    await new Promise(resolve => setTimeout(resolve, 1000));
  }

  throw new Error(`The API did not become healthy in time. See ${apiLogFile}`);
}

async function isUp(): Promise<boolean> {
  try {
    const response = await fetch(`${apiUrl}/health`);
    return response.ok;
  } catch {
    return false;
  }
}
