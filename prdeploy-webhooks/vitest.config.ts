import { defineConfig } from 'vitest/config';
import { resolve } from 'path';
import dotenv from 'dotenv';
import typescript from '@rollup/plugin-typescript';

dotenv.config({ path: './.env' });

// Use process.cwd() instead of __dirname for ESM compatibility
const rootDir = process.cwd();

export default defineConfig({
  plugins: [
    // Use @rollup/plugin-typescript to preserve decorator metadata for tsyringe
    // This must be placed early in the plugins array (as per Stack Overflow solution)
    // See: https://stackoverflow.com/questions/77616517/make-tsyringe-decorators-works-with-vite
    typescript({
      tsconfig: './tsconfig.spec.json',
      sourceMap: false // Disable sourcemaps to avoid warnings
    })
  ],
  test: {
    globals: true,
    environment: 'node',
    setupFiles: ['./test/setup.ts'],
    include: ['**/*.spec.ts'],
    testTimeout: 15000,
    clearMocks: true,
  },
  resolve: {
    alias: [
      { find: '@src', replacement: resolve(rootDir, './src') },
      { find: '@test', replacement: resolve(rootDir, './test') },
      // Mock problematic packages
      { find: 'mime', replacement: resolve(rootDir, './test/mocks/mime.ts') },
      { find: 'slackify-markdown', replacement: resolve(rootDir, './test/mocks/slackify-markdown.ts') },
    ],
  },
});

