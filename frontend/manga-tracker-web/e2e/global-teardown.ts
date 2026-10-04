import { execSync } from 'node:child_process';
import fs from 'node:fs';
import { apiPidFile } from './support/paths';

export default async function globalTeardown(): Promise<void> {
  if (!fs.existsSync(apiPidFile)) {
    return;
  }

  const pid = Number(fs.readFileSync(apiPidFile, 'utf8'));
  fs.rmSync(apiPidFile);

  try {
    // dotnet run starts the API as a child process: stop the whole tree.
    if (process.platform === 'win32') {
      execSync(`taskkill /PID ${pid} /T /F`, { stdio: 'ignore' });
    } else {
      process.kill(-pid, 'SIGTERM');
    }
  } catch {
    // Already stopped.
  }
}
