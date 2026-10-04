import path from 'node:path';

export const repoRoot = path.resolve(__dirname, '..', '..', '..', '..');
export const artifactsDir = path.resolve(__dirname, '..', '.artifacts');
export const apiLogFile = path.join(artifactsDir, 'api.log');
export const apiPidFile = path.join(artifactsDir, 'api.pid');
export const apiUrl = 'http://localhost:5243';
